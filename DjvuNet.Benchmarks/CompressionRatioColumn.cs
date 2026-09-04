using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace DjvuNet.Benchmarks
{
    public interface ICompressionRatioBenchmark
    {
        double GetCompressionRatio(BenchmarkCase benchmarkCase);
    }

    public class CompressionRatioColumn : IColumn
    {
        public string Id => nameof(CompressionRatioColumn);
        public string ColumnName => "Comp. Ratio";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 2;
        public bool IsNumeric => false; // False because it outputs a formatted string with parenthesis
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Ratio of Uncompressed Size to Compressed Size (and inverse %)";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            if (Activator.CreateInstance(benchmarkCase.Descriptor.Type) is ICompressionRatioBenchmark instance)
            {
                double ratio = instance.GetCompressionRatio(benchmarkCase);
                if (ratio > 0)
                {
                    double inversePercent = (1.0 / ratio) * 100.0;
                    return $"{ratio:N2} ({inversePercent:N2}%)";
                }
            }
            return "-";
        }
        
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
    }
}
