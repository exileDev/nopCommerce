using BenchmarkDotNet.Running;

namespace Nop.Benchmarks
{
    public class Program
    {
        static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<ProductServiceBenchmark>(Config.Instance);
		}
    }
}