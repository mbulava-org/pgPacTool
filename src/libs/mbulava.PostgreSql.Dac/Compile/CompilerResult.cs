using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Result of project compilation including errors, warnings, and deployment information
/// </summary>
public class CompilerResult
{
    /// <summary>
    /// Compilation errors that prevent deployment
    /// </summary>
    public List<CompilerError> Errors { get; set; } = new();

    /// <summary>
    /// Compilation warnings that should be reviewed
    /// </summary>
    public List<CompilerWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Circular dependencies found in the schema
    /// </summary>
    public List<CircularDependency> CircularDependencies { get; set; } = new();

    /// <summary>
    /// Dependency graph for the project
    /// </summary>
    public DependencyGraph? DependencyGraph { get; set; }

    /// <summary>
    /// Objects in safe deployment order (dependencies first)
    /// </summary>
    public List<string> DeploymentOrder { get; set; } = new();

    /// <summary>
    /// Objects grouped by deployment level (for parallel deployment)
    /// </summary>
    public List<List<string>> DeploymentLevels { get; set; } = new();

    /// <summary>
    /// Time taken to compile
    /// </summary>
    public TimeSpan CompilationTime { get; set; }

    /// <summary>
    /// True if compilation succeeded (no errors)
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// True if there are warnings
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// True if circular dependencies were found
    /// </summary>
    public bool HasCircularDependencies => CircularDependencies.Count > 0;

    /// <summary>
    /// Summary message
    /// </summary>
    public string GetSummary()
    {
        if (!IsSuccess)
        {
            return $"Compilation failed with {Errors.Count} error(s) and {Warnings.Count} warning(s).";
        }

        if (HasWarnings)
        {
            return $"Compilation succeeded with {Warnings.Count} warning(s). {DeploymentOrder.Count} objects ready for deployment.";
        }

        return $"Compilation succeeded. {DeploymentOrder.Count} objects ready for deployment.";
    }
}

/// <summary>
/// Compilation warning that should be reviewed
/// </summary>
public class CompilerWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Suggestion { get; set; }

    public CompilerWarning() { }

    public CompilerWarning(string code, string message, string location, string? suggestion = null)
    {
        Code = code;
        Message = message;
        Location = location;
        Suggestion = suggestion;
    }
}

