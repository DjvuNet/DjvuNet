using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace DjvuNet.Shared.Tests
{
    public enum Platform
    {
        AnyCpu = 0,
        X86 = 1,
        X64 = 2,
        Arm = 3,
        Arm64 = 4
    }

    // Ported from BenchmarkDotNet (MIT License)
    // based on https://github.com/dotnet/runtime/tree/v10.0.0-rc.1.25451.107/src/coreclr/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
    public static class HardwareIntrinsics
    {
        public static string GetHardwareVectorSize()
        {
            if (System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated) return "MaxVectorSize=512";
            if (System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated) return "MaxVectorSize=256";
            if (System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated) return "MaxVectorSize=128";
            if (System.Runtime.Intrinsics.Vector64.IsHardwareAccelerated) return "MaxVectorSize=64";
            return string.Empty;
        }

        public static string GetShortInfo()
        {
            if (IsX86BaseSupported)
            {
                if (IsX86Avx512Supported) return "x86-64-v4";
                if (IsX86Avx2Supported) return "x86-64-v3";
                if (IsX86Sse42Supported) return "x86-64-v2";
                
                return "x86-64-v1";
            }
            
            if (IsArmBaseSupported)
            {
                return "armv8.0-a";
            }
            
            return GetHardwareVectorSize(); 
        }

        public static string GetFullInfo(Platform platform)
        {
            return string.Join(",", GetCurrentProcessInstructionSets(platform));

            static IEnumerable<string> GetCurrentProcessInstructionSets(Platform platform)
            {
                switch (platform)
                {
                    case Platform.X86:
                    case Platform.X64:
                        {
                            if (IsX86Avx10v2Supported) yield return "AVX10v2";
                            if (IsX86Avx10v1Supported)
                            {
                                yield return "AVX10v1";
                                yield return "AVX512 BF16+FP16";
                            }
                            if (IsX86Avx512v3Supported) yield return "AVX512 BITALG+VBMI2+VNNI+VPOPCNTDQ";
                            if (IsX86Avx512v2Supported) yield return "AVX512 IFMA+VBMI";
                            if (IsX86Avx512Supported) yield return "AVX512 F+BW+CD+DQ+VL";
                            if (IsX86Avx2Supported) yield return "AVX2+BMI1+BMI2+F16C+FMA+LZCNT+MOVBE";
                            if (IsX86AvxSupported) yield return "AVX";
                            if (IsX86Sse42Supported) yield return "SSE3+SSSE3+SSE4.1+SSE4.2+POPCNT";
                            if (IsX86BaseSupported) yield return "X86Base+SSE+SSE2";
                            if (IsX86AesSupported) yield return "AES+PCLMUL";
                            if (IsX86AvxVnniSupported) yield return "AvxVnni";
                            if (IsX86SerializeSupported) yield return "SERIALIZE";
                            break;
                        }
                    case Platform.Arm64:
                        {
                            if (IsArmBaseSupported) yield return "ArmBase+AdvSimd";
                            if (IsArmAesSupported) yield return "AES";
                            if (IsArmCrc32Supported) yield return "CRC32";
                            if (IsArmDpSupported) yield return "DP";
                            if (IsArmRdmSupported) yield return "RDM";
                            if (IsArmSha1Supported) yield return "SHA1";
                            if (IsArmSha256Supported) yield return "SHA256";
                            break;
                        }
                    default:
                        yield break;
                }
            }
        }

#pragma warning disable CA2252 // Some APIs require opting into preview features
        public static bool IsX86BaseSupported => X86Base.IsSupported && Sse.IsSupported && Sse2.IsSupported;
        public static bool IsX86Sse42Supported => Sse3.IsSupported && Ssse3.IsSupported && Sse41.IsSupported && Sse42.IsSupported && Popcnt.IsSupported;
        public static bool IsX86AvxSupported => Avx.IsSupported;
        public static bool IsX86Avx2Supported => Avx2.IsSupported && Bmi1.IsSupported && Bmi2.IsSupported && Fma.IsSupported && Lzcnt.IsSupported;
        public static bool IsX86Avx512Supported => Avx512F.IsSupported && Avx512F.VL.IsSupported && Avx512BW.IsSupported && Avx512BW.VL.IsSupported && Avx512CD.IsSupported && Avx512CD.VL.IsSupported && Avx512DQ.IsSupported && Avx512DQ.VL.IsSupported;
        public static bool IsX86Avx512v2Supported => Avx512Vbmi.IsSupported && Avx512Vbmi.VL.IsSupported;
        public static bool IsX86Avx512v3Supported => Avx512Vbmi2.IsSupported && Avx512Vbmi2.VL.IsSupported;
        public static bool IsX86Avx10v1Supported => Avx10v1.IsSupported && Avx10v1.V512.IsSupported;
        public static bool IsX86Avx10v2Supported => Avx10v2.IsSupported && Avx10v2.V512.IsSupported;
        public static bool IsX86AesSupported => System.Runtime.Intrinsics.X86.Aes.IsSupported && Pclmulqdq.IsSupported;
        public static bool IsX86AvxVnniSupported => AvxVnni.IsSupported;
        public static bool IsX86SerializeSupported => X86Serialize.IsSupported;
        
        public static bool IsArmBaseSupported => ArmBase.IsSupported && AdvSimd.IsSupported;
        public static bool IsArmAesSupported => System.Runtime.Intrinsics.Arm.Aes.IsSupported;
        public static bool IsArmCrc32Supported => Crc32.IsSupported;
        public static bool IsArmDpSupported => Dp.IsSupported;
        public static bool IsArmRdmSupported => Rdm.IsSupported;
        public static bool IsArmSha1Supported => Sha1.IsSupported;
        public static bool IsArmSha256Supported => Sha256.IsSupported;
#pragma warning restore CA2252
    }
}
