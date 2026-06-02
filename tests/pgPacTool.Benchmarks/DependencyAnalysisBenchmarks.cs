using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using PgQuery;

namespace pgPacTool.Benchmarks;

/// <summary>
/// Benchmarks for DependencyAnalyzer — measures time to collect and resolve
/// dependency graphs of various project sizes.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class DependencyAnalysisBenchmarks
{
    private PgProject _smallProject = null!;
    private PgProject _mediumProject = null!;
    private PgProject _largeProject = null!;
    private DependencyAnalyzer _analyzer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _analyzer = new DependencyAnalyzer();
        _smallProject  = ProjectFactory.Create(tableCount: 5,   viewCount: 2,  functionCount: 2);
        _mediumProject = ProjectFactory.Create(tableCount: 50,  viewCount: 20, functionCount: 20);
        _largeProject  = ProjectFactory.Create(tableCount: 200, viewCount: 80, functionCount: 80);
    }

    [Benchmark(Description = "Analyze dependencies — small project")]
    public void AnalyzeSmall() => _analyzer.AnalyzeProject(_smallProject);

    [Benchmark(Description = "Analyze dependencies — medium project")]
    public void AnalyzeMedium() => _analyzer.AnalyzeProject(_mediumProject);

    [Benchmark(Description = "Analyze dependencies — large project")]
    public void AnalyzeLarge() => _analyzer.AnalyzeProject(_largeProject);
}
