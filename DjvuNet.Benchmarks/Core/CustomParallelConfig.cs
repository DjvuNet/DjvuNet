using BenchmarkDotNet.Configs;

namespace DjvuNet.Benchmarks.Core
{
    public class CustomParallelConfig : StandardConfig
    {
        public CustomParallelConfig()
        {
            AddColumn(new PerOperationTimeColumn());
        }
    }
}
