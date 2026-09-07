using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace DjvuNet.Benchmarks.Core
{
    public class PerOperationTimeColumn : IColumn
    {
        public string Id => nameof(PerOperationTimeColumn);
        public string ColumnName => "Time/Op (Real)";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Time;
        public string Legend => "Time per single PixelCount invocation";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var mean = summary.Reports.FirstOrDefault(r => r.BenchmarkCase == benchmarkCase)?.ResultStatistics?.Mean;
            if (!mean.HasValue) return "N/A";

            int pixelCount = (int)benchmarkCase.Parameters["PixelCount"];
            int maxPixels = 5448 * 3686;
            int imageRatio = maxPixels / pixelCount;
            int totalOperations = imageRatio;

            double timePerOpNs = mean.Value / totalOperations;

            if (timePerOpNs >= 1_000_000) return (timePerOpNs / 1_000_000.0).ToString("N4") + " ms";
            if (timePerOpNs >= 1_000) return (timePerOpNs / 1_000.0).ToString("N4") + " us";
            return timePerOpNs.ToString("N4") + " ns";
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
    }
}
