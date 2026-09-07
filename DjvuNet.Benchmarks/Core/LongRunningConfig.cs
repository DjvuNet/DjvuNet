using System;
using System.IO;
using DjvuNet.Tests;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace DjvuNet.Benchmarks.Core
{
    public class LongRunningConfig : ManualConfig
    {
        public static readonly string BenchmarksArtifactsPath = Path.Combine(Util.RepoRoot, "TestResults", "Benchmarks", "Baselines");

        public LongRunningConfig()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddExporter(MarkdownExporter.Console);
            AddExporter(JsonExporter.Full);
            ArtifactsPath = BenchmarksArtifactsPath;

            // Use In-Process toolchain to bypass the auto-generated .csproj
            // which breaks due to our custom Directory.Build.props output paths.
            // Explicitly enable Server GC as a benchmark configuration option.
            // Increase the timeout drastically because the 34s decoding triggers the default timeout.
            var toolchain = new InProcessEmitToolchain(
                timeout: TimeSpan.FromMinutes(30),
                logOutput: true);

            AddJob(Job.ShortRun
                .WithToolchain(toolchain)
                .WithGcServer(true)
                .WithStrategy(BenchmarkDotNet.Engines.RunStrategy.ColdStart)
                .WithWarmupCount(4)
                .WithIterationCount(11));
        }
    }
}
