using mbulava.PostgreSql.Dac.Models;
using System.Diagnostics;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Compiles and validates PostgreSQL database projects
/// </summary>
public class ProjectCompiler
{
    private readonly DependencyAnalyzer _analyzer;
    private readonly CircularDependencyDetector _cycleDetector;
    private readonly TopologicalSorter _sorter;

    public ProjectCompiler()
    {
        _analyzer = new DependencyAnalyzer();
        _cycleDetector = new CircularDependencyDetector();
        _sorter = new TopologicalSorter();
    }

    /// <summary>
    /// Compiles a PostgreSQL project and validates dependencies
    /// </summary>
    public CompilerResult Compile(PgProject project)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CompilerResult();

        try
        {
            // Step 1: Build dependency graph
            result.DependencyGraph = _analyzer.AnalyzeProject(project);

            // Step 2: Detect circular dependencies
            result.CircularDependencies = _cycleDetector.DetectCycles(result.DependencyGraph);

            // If there are error-level cycles, stop here
            var errorCycles = result.CircularDependencies.Where(c => c.Severity == CycleSeverity.Error).ToList();
            if (errorCycles.Any())
            {
                foreach (var cycle in errorCycles)
                {
                    result.Errors.Add(new CompilerError(
                        "CYCLE001",
                        cycle.Description,
                        string.Join(", ", cycle.Cycle)
                    ));
                }

                stopwatch.Stop();
                result.CompilationTime = stopwatch.Elapsed;
                return result;
            }

            // Add warnings for warning-level cycles
            var warningCycles = result.CircularDependencies.Where(c => c.Severity == CycleSeverity.Warning).ToList();
            foreach (var cycle in warningCycles)
            {
                result.Warnings.Add(new CompilerWarning(
                    "CYCLE002",
                    cycle.Description,
                    string.Join(", ", cycle.Cycle),
                    cycle.Suggestion
                ));
            }

            // Add info for info-level cycles (self-references that are allowed)
            var infoCycles = result.CircularDependencies.Where(c => c.Severity == CycleSeverity.Info).ToList();
            foreach (var cycle in infoCycles)
            {
                result.Warnings.Add(new CompilerWarning(
                    "CYCLE003",
                    cycle.Description,
                    string.Join(", ", cycle.Cycle),
                    cycle.Suggestion
                ));
            }

            // Step 3: Topological sort for deployment order
            try
            {
                // For self-references (single-node cycles), we can still sort
                // Remove self-loops temporarily for sorting
                var sortableGraph = RemoveSelfLoops(result.DependencyGraph);
                result.DeploymentOrder = _sorter.Sort(sortableGraph);
                result.DeploymentLevels = _sorter.SortInLevels(sortableGraph);
            }
            catch (InvalidOperationException ex)
            {
                result.Errors.Add(new CompilerError(
                    "SORT001",
                    "Cannot determine deployment order due to circular dependencies",
                    ex.Message
                ));
            }

            // Step 4: Additional validations could go here
            // - Reference validation
            // - Type validation
            // - Privilege validation
            // - Schema validation

            stopwatch.Stop();
            result.CompilationTime = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new CompilerError(
                "COMP001",
                "Compilation failed with unexpected error",
                ex.Message
            ));

            stopwatch.Stop();
            result.CompilationTime = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Quick validation check - returns true if project can be compiled
    /// </summary>
    public bool CanCompile(PgProject project)
    {
        var result = Compile(project);
        return result.IsSuccess;
    }

    /// <summary>
    /// Removes self-loops from graph for topological sorting (self-references are allowed)
    /// </summary>
    private DependencyGraph RemoveSelfLoops(DependencyGraph originalGraph)
    {
        var newGraph = new DependencyGraph();
        var allObjects = originalGraph.GetAllObjects();

        // Add all objects
        foreach (var obj in allObjects)
        {
            var type = originalGraph.GetObjectType(obj);
            if (type != null)
            {
                newGraph.AddObject(obj, type);
            }
        }

        // Add dependencies, but skip self-loops
        foreach (var obj in allObjects)
        {
            var deps = originalGraph.GetDependencies(obj);
            foreach (var dep in deps)
            {
                if (obj != dep) // Skip self-loop
                {
                    newGraph.AddDependency(obj, dep);
                }
            }
        }

        return newGraph;
    }
}

