using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Detects circular dependencies in a dependency graph using Tarjan's algorithm
/// </summary>
public class CircularDependencyDetector
{
    /// <summary>
    /// Detects all circular dependencies in the graph
    /// </summary>
    public List<CircularDependency> DetectCycles(DependencyGraph graph)
    {
        var cycles = new List<CircularDependency>();
        var allCyclePaths = FindAllCycles(graph);
        
        foreach (var cyclePath in allCyclePaths)
        {
            var cycle = CreateCircularDependency(graph, cyclePath);
            cycles.Add(cycle);
        }
        
        return cycles;
    }
    
    /// <summary>
    /// Quick check if graph has any cycles
    /// </summary>
    public bool HasCycles(DependencyGraph graph)
    {
        var allObjects = graph.GetAllObjects();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        
        foreach (var obj in allObjects)
        {
            if (!visited.Contains(obj))
            {
                if (HasCycleDFS(graph, obj, visited, recursionStack))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Finds all cycle paths in the graph
    /// </summary>
    public List<List<string>> FindAllCycles(DependencyGraph graph)
    {
        var allCycles = new List<List<string>>();
        var allObjects = graph.GetAllObjects();
        var visited = new HashSet<string>();
        var recursionStack = new Stack<string>();
        
        foreach (var obj in allObjects)
        {
            if (!visited.Contains(obj))
            {
                FindCyclesDFS(graph, obj, visited, recursionStack, allCycles);
            }
        }
        
        return allCycles;
    }
    
    /// <summary>
    /// DFS to check if there's a cycle from current node
    /// </summary>
    private bool HasCycleDFS(DependencyGraph graph, string current, 
        HashSet<string> visited, HashSet<string> recursionStack)
    {
        visited.Add(current);
        recursionStack.Add(current);
        
        var dependencies = graph.GetDependencies(current);
        foreach (var dep in dependencies)
        {
            if (!visited.Contains(dep))
            {
                if (HasCycleDFS(graph, dep, visited, recursionStack))
                {
                    return true;
                }
            }
            else if (recursionStack.Contains(dep))
            {
                // Found a cycle
                return true;
            }
        }
        
        recursionStack.Remove(current);
        return false;
    }
    
    /// <summary>
    /// DFS to find all cycles and their paths
    /// </summary>
    private void FindCyclesDFS(DependencyGraph graph, string current,
        HashSet<string> visited, Stack<string> recursionStack, List<List<string>> allCycles)
    {
        visited.Add(current);
        recursionStack.Push(current);
        
        var dependencies = graph.GetDependencies(current);
        foreach (var dep in dependencies)
        {
            if (!visited.Contains(dep))
            {
                FindCyclesDFS(graph, dep, visited, recursionStack, allCycles);
            }
            else if (recursionStack.Contains(dep))
            {
                // Found a cycle - extract the cycle path
                var cycle = ExtractCyclePath(recursionStack, dep);
                
                // Check if we've already found this cycle (avoid duplicates)
                if (!IsDuplicateCycle(allCycles, cycle))
                {
                    allCycles.Add(cycle);
                }
            }
        }
        
        recursionStack.Pop();
    }
    
    /// <summary>
    /// Extracts the cycle path from the recursion stack
    /// </summary>
    private List<string> ExtractCyclePath(Stack<string> recursionStack, string cycleStart)
    {
        var cycle = new List<string>();
        var foundStart = false;
        
        // Stack is LIFO, so reverse to get correct order
        foreach (var item in recursionStack.Reverse())
        {
            if (item == cycleStart)
            {
                foundStart = true;
            }
            
            if (foundStart)
            {
                cycle.Add(item);
            }
        }
        
        return cycle;
    }
    
    /// <summary>
    /// Checks if a cycle is already in the list (handles different orderings of same cycle)
    /// </summary>
    private bool IsDuplicateCycle(List<List<string>> allCycles, List<string> newCycle)
    {
        if (newCycle.Count == 0)
            return false;
        
        foreach (var existingCycle in allCycles)
        {
            if (AreSameCycle(existingCycle, newCycle))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if two cycles are the same (ignoring rotation)
    /// </summary>
    private bool AreSameCycle(List<string> cycle1, List<string> cycle2)
    {
        if (cycle1.Count != cycle2.Count)
            return false;
        
        if (cycle1.Count == 0)
            return true;
        
        // Check if cycle2 is a rotation of cycle1
        var cycle1Set = new HashSet<string>(cycle1);
        var cycle2Set = new HashSet<string>(cycle2);
        
        // Must have same elements
        if (!cycle1Set.SetEquals(cycle2Set))
            return false;
        
        // Check all rotations
        for (int offset = 0; offset < cycle1.Count; offset++)
        {
            bool matches = true;
            for (int i = 0; i < cycle1.Count; i++)
            {
                if (cycle1[i] != cycle2[(i + offset) % cycle2.Count])
                {
                    matches = false;
                    break;
                }
            }
            
            if (matches)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Creates a CircularDependency object from a cycle path
    /// </summary>
    private CircularDependency CreateCircularDependency(DependencyGraph graph, List<string> cyclePath)
    {
        var cycle = new CircularDependency
        {
            Cycle = cyclePath
        };
        
        // Extract object types
        foreach (var obj in cyclePath)
        {
            var type = graph.GetObjectType(obj);
            if (type != null)
            {
                cycle.ObjectTypes.Add(type);
            }
        }
        
        // Determine severity and suggestions
        AnalyzeCycle(cycle);
        
        // Generate description
        cycle.Description = GenerateDescription(cycle);
        
        return cycle;
    }
    
    /// <summary>
    /// Analyzes a cycle to determine severity and provide suggestions
    /// </summary>
    private void AnalyzeCycle(CircularDependency cycle)
    {
        // Self-reference (single object)
        if (cycle.Cycle.Count == 1)
        {
            var type = cycle.ObjectTypes.FirstOrDefault();

            if (type == "FUNCTION")
            {
                // Recursive functions are allowed
                cycle.Severity = CycleSeverity.Info;
                cycle.Suggestion = "Recursive function - this is allowed in PostgreSQL.";
            }
            else if (type == "TABLE")
            {
                // Self-referential FK is allowed (parent-child in same table)
                cycle.Severity = CycleSeverity.Info;
                cycle.Suggestion = "Self-referential foreign key - this is allowed but ensure proper constraints.";
            }
            else
            {
                cycle.Severity = CycleSeverity.Info;
                cycle.Suggestion = "Self-reference detected - verify this is intentional.";
            }

            return;
        }
        
        // Check for view cycles (not allowed)
        if (cycle.ObjectTypes.All(t => t == "VIEW"))
        {
            cycle.Severity = CycleSeverity.Error;
            cycle.Suggestion = "Views cannot have circular dependencies. Redesign one view to remove the cycle.";
            return;
        }
        
        // Check for mixed view/table cycles
        if (cycle.ObjectTypes.Contains("VIEW"))
        {
            cycle.Severity = CycleSeverity.Error;
            cycle.Suggestion = "Circular dependency involving views. Views must reference tables/views without cycles.";
            return;
        }
        
        // Table cycles (foreign keys)
        if (cycle.ObjectTypes.All(t => t == "TABLE"))
        {
            // All table cycles with FKs are errors - they need DEFERRABLE constraints
            // or schema redesign to work properly
            cycle.Severity = CycleSeverity.Error;

            if (cycle.Cycle.Count == 2)
            {
                cycle.Suggestion = "Two tables with circular foreign keys. Use DEFERRABLE constraints or break the cycle.";
            }
            else
            {
                cycle.Suggestion = $"Complex circular foreign keys ({cycle.Cycle.Count} tables). Break the cycle by removing one foreign key or redesigning the schema.";
            }
            return;
        }
        
        // Default case
        cycle.Severity = CycleSeverity.Warning;
        cycle.Suggestion = "Circular dependency detected. Review and break the cycle if possible.";
    }
    
    /// <summary>
    /// Generates a human-readable description
    /// </summary>
    private string GenerateDescription(CircularDependency cycle)
    {
        if (cycle.Cycle.Count == 0)
            return "Empty cycle";
        
        if (cycle.Cycle.Count == 1)
        {
            return $"Self-reference: {cycle.Cycle[0]}";
        }
        
        var types = string.Join(", ", cycle.ObjectTypes.Distinct());
        return $"Circular dependency ({types}): {cycle.GetCyclePath()}";
    }
}
