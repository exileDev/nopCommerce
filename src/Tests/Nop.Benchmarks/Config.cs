using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace Nop.Benchmarks
{
	public static class Config
	{
		public static IConfig Instance { get; } = Create();

		private static IConfig Create()
		{
			return new ManualConfig()
				.AddLogger         (DefaultConfig.Instance.GetLoggers        ().ToArray())
				.AddAnalyser       (DefaultConfig.Instance.GetAnalysers      ().ToArray())
				.AddValidator      (DefaultConfig.Instance.GetValidators     ().ToArray())
				.AddColumnProvider (DefaultConfig.Instance.GetColumnProviders().Select(p => new FilteredColumnProvider(p)).ToArray())
				.WithOptions       (ConfigOptions.DisableLogFile)
				.AddExporter       (MarkdownExporter.GitHub)
				.AddDiagnoser      (MemoryDiagnoser.Default)
				.WithArtifactsPath (@"..\..\..")
				.AddJob            (Job.Default.WithRuntime(CoreRuntime.Core31).WithDefault());
		}

		private static Job WithDefault(this Job job)
		{
			return job.WithJit(Jit.RyuJit)
				.WithPlatform(Platform.X64);
		}

		class FilteredColumnProvider : IColumnProvider
		{
			private readonly IColumnProvider _provider;
			public FilteredColumnProvider(IColumnProvider provider)
			{
				_provider = provider;
			}

			IEnumerable<IColumn> IColumnProvider.GetColumns(Summary summary)
			{
				return _provider
					.GetColumns(summary)
					// Job is not useful at all, other columns could be enabled later if somebody will find them useful
					.Where(c => c.ColumnName != "Job"
							&& c.ColumnName != "Error"
							&& c.ColumnName != "StdDev"
							&& c.ColumnName != "RatioSD");
			}
		}
	}
}
