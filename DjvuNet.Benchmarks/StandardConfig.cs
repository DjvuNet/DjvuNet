using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using DjvuNet.Tests;
using DjvuNet;

namespace DjvuNet.Benchmarks
{
    public class StandardConfig : ManualConfig
    {
        public StandardConfig() : this(true) { }

        protected StandardConfig(bool addDefaultJob)
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportGithubMarkdown: true, exportHtml: true)));

            var attr = typeof(DjvuDocument).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            string versionStr = attr != null ? attr.InformationalVersion : typeof(DjvuDocument).Assembly.GetName().Version.ToString();

            string safeVersion = versionStr.Replace('+', '-').Replace(' ', '_');
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safeVersion = safeVersion.Replace(c.ToString(), "");
            }
            if (safeVersion.Length > 50)
            {
                int cutoff = safeVersion.IndexOf("_@Branch");
                if (cutoff > 0) safeVersion = safeVersion.Substring(0, cutoff);
                else safeVersion = safeVersion.Substring(0, 50);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            AddLogger(new SummaryHeaderLogger(versionStr, timestamp));

            ArtifactsPath = Path.Combine(Util.RepoRoot, "TestResults", "Benchmarks");

            AddExporter(MarkdownExporter.Console);
            AddExporter(JsonExporter.Full);

            AddColumn(new ThroughputColumn());
            AddColumn(new CompressionRatioColumn());
            this.HideColumns("Error", "EnvironmentVariables", "RatioSD");

            var toolchain = new InProcessEmitToolchain(
                timeout: TimeSpan.FromMinutes(30),
                logOutput: false);

            if (addDefaultJob)
            {
                // ORIGINAL ACCURACY CONFIGURATION (Longer execution time)
                AddJob(Job.Default
                    .WithToolchain(toolchain)
                    .WithGcServer(true));
            }

            // FAST / ACCURATE BALANCED CONFIGURATION
            // AddJob(Job.ShortRun
            //     .WithToolchain(toolchain)
            //     .WithGcServer(true)
            //     .WithMinIterationTime(Perfolizer.Horology.TimeInterval.FromMilliseconds(250)));
        }

        private class SummaryHeaderLogger : ILogger
        {
            private readonly string _versionInfo;
            private readonly string _timestamp;
            public SummaryHeaderLogger(string versionInfo, string timestamp)
            {
                _versionInfo = versionInfo;
                _timestamp = timestamp;
            }

            public string Id => "SummaryHeaderLogger";
            public int Priority => 0;
            public void Write(LogKind logKind, string text)
            {
                if (text.Contains("// * Summary *"))
                {
                    Console.WriteLine();
                    Console.WriteLine($"  [DjvuNet] : Version {_versionInfo}");
                    Console.WriteLine($"  [RunTime] : {_timestamp}");
                    Console.WriteLine();
                }
            }
            public void WriteLine() { }
            public void WriteLine(LogKind logKind, string text) => Write(logKind, text);
            public void Flush() { }
        }
    }
}