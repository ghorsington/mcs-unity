using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.CSharp
{
    internal static class SkipVisibilityExt
    {
        internal static readonly bool _IsMono = Type.GetType("Mono.Runtime") != null;
        internal static readonly bool _IsNewMonoSRE = _IsMono && typeof(DynamicMethod).GetField("il_info", BindingFlags.NonPublic | BindingFlags.Instance) != null;
        internal static readonly bool _IsOldMonoSRE = _IsMono && !_IsNewMonoSRE && typeof(DynamicMethod).GetField("ilgen", BindingFlags.NonPublic | BindingFlags.Instance) != null;

        internal static FieldInfo dynAssField =
            typeof(AssemblyBuilder).GetField("dynamic_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public static unsafe void MarkCorlibInternal(this AssemblyBuilder asm)
        {
            if (!_IsMono || dynAssField == null)
                return;

            var asmPtr = (UIntPtr) dynAssField.GetValue(asm);

            if (asmPtr == UIntPtr.Zero)
                return;
            
            var offs = 0;

            if (_IsOldMonoSRE)
            {
                offs =
                    // ref_count (4 + padding)
                    IntPtr.Size +
                    // basedir
                    IntPtr.Size +

                    // aname
                    // name
                    IntPtr.Size +
                    // culture
                    IntPtr.Size +
                    // hash_value
                    IntPtr.Size +
                    // public_key
                    IntPtr.Size +
                    // public_key_token (17 + padding)
                    17 + 3 +
                    // hash_alg
                    4 +
                    // hash_len
                    4 +
                    // flags
                    4 +

                    // major, minor, build, revision
                    2 + 2 + 2 + 2 +

                    // image
                    IntPtr.Size +
                    // friend_assembly_names
                    IntPtr.Size +
                    // friend_assembly_names_inited
                    1 +
                    // in_gac
                    1 +
                    // dynamic
                    1;
            } else if (_IsNewMonoSRE)
            {
                offs =
                    // ref_count (4 + padding)
                    IntPtr.Size +
                    // basedir
                    IntPtr.Size +

                    // aname
                    // name
                    IntPtr.Size +
                    // culture
                    IntPtr.Size +
                    // hash_value
                    IntPtr.Size +
                    // public_key
                    IntPtr.Size +
                    // public_key_token (17 + padding)
                    20 +
                    // hash_alg
                    4 +
                    // hash_len
                    4 +
                    // flags
                    4 +

                    // major, minor, build, revision, arch (10 framework / 20 core + padding)
                    (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib" ? IntPtr.Size == 4
                            ?
                            20
                            : 24 :
                        IntPtr.Size == 4 ? 12 : 16) +

                    // image
                    IntPtr.Size +
                    // friend_assembly_names
                    IntPtr.Size +
                    // friend_assembly_names_inited
                    1 +
                    // in_gac
                    1 +
                    // dynamic
                    1;
            }

            if (offs == 0)
                return;
            
            var corlibInternalPtr = (byte*) ((long) asmPtr + offs);
            *corlibInternalPtr = 1;
        }
    }
}