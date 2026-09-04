using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace DjvuNet.Benchmarks
{
    public interface IThroughputBenchmark
    {
        /// <summary>
        /// Calculates the total bytes processed in a single BDN operation.
        /// Executed in the BDN Host Process during report generation.
        /// </summary>
        long GetBytesPerOperation(BenchmarkCase benchmarkCase);
    }

    public class ThroughputColumn : IColumn
    {
        public string Id => nameof(ThroughputColumn);
        public string ColumnName => "GB/s";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 1;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Throughput in GB/s (1024^3 bytes)";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var type = benchmarkCase.Descriptor.Type;
            if (!typeof(IThroughputBenchmark).IsAssignableFrom(type))
                return "-";

            var meanNs = summary.Reports.FirstOrDefault(r => r.BenchmarkCase == benchmarkCase)?.ResultStatistics?.Mean;
            if (!meanNs.HasValue || meanNs.Value == 0) return "N/A";

            long bytesPerOp = 0;
            try
            {
                // Instantiate the benchmark class (constructors are lightweight per BDN guidelines)
                if (Activator.CreateInstance(type) is IThroughputBenchmark instance)
                {
                    bytesPerOp = instance.GetBytesPerOperation(benchmarkCase);
                }
            }
            catch { return "Error"; }

            if (bytesPerOp == 0) return "-";

            // Convert nanoseconds to seconds
            double seconds = meanNs.Value / 1_000_000_000.0;
            // Binary GB: 1024^3 bytes
            double gbProcessed = bytesPerOp / 1073741824.0;
            
            return (gbProcessed / seconds).ToString("N3");
        }
        
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
    }
}
