namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Severity level for circular dependencies
/// </summary>
public enum CycleSeverity
{
    /// <summary>
    /// Allowed circular dependency (e.g., self-referential FK, recursive function)
    /// </summary>
    Info,
    
    /// <summary>
    /// Potentially problematic but might work
    /// </summary>
    Warning,
    
    /// <summary>
    /// Not allowed - will fail deployment
    /// </summary>
    Error
}

/// <summary>
/// Represents a circular dependency in the database schema
/// </summary>
public class CircularDependency
{
    /// <summary>
    /// The objects involved in the cycle
    /// </summary>
    public List<string> Cycle { get; set; } = new();
    
    /// <summary>
    /// Types of objects in the cycle (TABLE, VIEW, etc.)
    /// </summary>
    public List<string> ObjectTypes { get; set; } = new();
    
    /// <summary>
    /// Severity of this circular dependency
    /// </summary>
    public CycleSeverity Severity { get; set; }
    
    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Suggestion for how to break the cycle
    /// </summary>
    public string Suggestion { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets a formatted cycle path (A → B → C → A)
    /// </summary>
    public string GetCyclePath()
    {
        if (Cycle.Count == 0)
            return string.Empty;
        
        return string.Join(" → ", Cycle) + " → " + Cycle[0];
    }
}
