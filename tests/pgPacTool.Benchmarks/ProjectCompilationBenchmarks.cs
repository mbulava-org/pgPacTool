using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;

namespace pgPacTool.Benchmarks;

/// <summary>
/// Benchmarks for ProjectCompiler — measures time to compile PostgreSQL
/// projects of varying sizes through dependency analysis and topological sort.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class ProjectCompilationBenchmarks
{
    private PgProject _smallProject = null!;
    private PgProject _mediumProject = null!;
    private PgProject _largeProject = null!;
    private ProjectCompiler _compiler = null!;

    [GlobalSetup]
    public void Setup()
    {
        _compiler = new ProjectCompiler();
        _smallProject  = ProjectFactory.Create(tableCount: 5,   viewCount: 2,  functionCount: 2);
        _mediumProject = ProjectFactory.Create(tableCount: 50,  viewCount: 20, functionCount: 20);
        _largeProject  = ProjectFactory.Create(tableCount: 200, viewCount: 80, functionCount: 80);
    }

    [Benchmark(Description = "Compile small project (5 tables, 2 views, 2 functions)")]
    public void CompileSmallProject() => _compiler.Compile(_smallProject);

    [Benchmark(Description = "Compile medium project (50 tables, 20 views, 20 functions)")]
    public void CompileMediumProject() => _compiler.Compile(_mediumProject);

    [Benchmark(Description = "Compile large project (200 tables, 80 views, 80 functions)")]
    public void CompileLargeProject() => _compiler.Compile(_largeProject);
}
