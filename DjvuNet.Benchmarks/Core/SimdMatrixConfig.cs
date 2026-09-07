using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace DjvuNet.Benchmarks.Core
{
    /// <summary>
    /// Custom configuration using Out-Of-Process Jobs to force the JIT to respect CPU flag environment variables.
    /// This matrix enforces benchmark execution across 4 distinct instruction set tiers (Scalar, Vector128, AVX2, AVX512).
    /// </summary>
    public class SimdMatrixConfig : StandardConfig
    {
        public SimdMatrixConfig() : this(Job.Default)
        {
        }

        protected SimdMatrixConfig(Job baseJob) : base(false)
        {
            // 1. Baseline: Scalar (Disable all hardware intrinsics)
            AddJob(baseJob
                .WithGcServer(true)
                .WithId("1. Scalar")
                .WithEnvironmentVariable("DOTNET_EnableHWIntrinsic", "0")
                .AsBaseline());

            // 2. Vector128: (SSE / SSE4.1) by disabling AVX and higher
            AddJob(baseJob
                .WithGcServer(true)
                .WithId("2. Vector128")
                .WithEnvironmentVariable("DOTNET_EnableAVX", "0"));

            // 3. AVX2: Disable AVX-512 to restrict pipeline to 256-bit
            AddJob(baseJob
                .WithGcServer(true)
                .WithId("3. AVX2")
                .WithEnvironmentVariable("DOTNET_EnableAVX512", "0"));

            // 4. AVX512: Unrestricted (Maximum Hardware Capabilities)
            AddJob(baseJob
                .WithGcServer(true)
                .WithId("4. AVX512"));
        }
    }

    /// <summary>
    /// Executes the SIMD matrix using Job.ShortRun (1 launch, 1 warmup, 3 target iterations) for rapid developer loops.
    /// </summary>
    public class ShortSimdMatrixConfig : SimdMatrixConfig
    {
        public ShortSimdMatrixConfig() : base(Job.ShortRun)
        {
            this.HideColumns("Comp. Ratio");
        }
    }
}
