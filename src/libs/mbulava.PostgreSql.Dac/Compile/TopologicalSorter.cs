using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Performs topological sorting on dependency graphs using Kahn's algorithm
/// </summary>
public class TopologicalSorter
{
    /// <summary>
    /// Sorts objects in dependency order (dependencies first)
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if graph contains cycles</exception>
    public List<string> Sort(DependencyGraph graph)
    {
        var allObjects = graph.GetAllObjects();
        if (allObjects.Count == 0)
            return new List<string>();
        
        // Calculate in-degree for each node
        var inDegree = CalculateInDegree(graph, allObjects);
        
        // Queue of nodes with no incoming edges
        var queue = new Queue<string>();
        foreach (var obj in allObjects)
        {
            if (inDegree[obj] == 0)
            {
                queue.Enqueue(obj);
            }
        }
        
        var sorted = new List<string>();
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);
            
            // For each object that depends on current (dependents)
            var dependents = graph.GetDependents(current);
            foreach (var dependent in dependents)
            {
                inDegree[dependent]--;
                
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }
        
        // If we didn't process all nodes, there's a cycle
        if (sorted.Count != allObjects.Count)
        {
            throw new InvalidOperationException(
                $"Cannot perform topological sort: graph contains circular dependencies. " +
                $"Processed {sorted.Count} of {allObjects.Count} objects.");
        }
        
        return sorted;
    }
    
    /// <summary>
    /// Sorts objects into levels where objects in the same level can be created in parallel
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if graph contains cycles</exception>
    public List<List<string>> SortInLevels(DependencyGraph graph)
    {
        var allObjects = graph.GetAllObjects();
        if (allObjects.Count == 0)
            return new List<List<string>>();
        
        // Calculate in-degree for each node
        var inDegree = CalculateInDegree(graph, allObjects);
        
        // Queue of nodes with no incoming edges
        var queue = new Queue<string>();
        foreach (var obj in allObjects)
        {
            if (inDegree[obj] == 0)
            {
                queue.Enqueue(obj);
            }
        }
        
        var levels = new List<List<string>>();
        int processedCount = 0;
        
        while (queue.Count > 0)
        {
            // Process all nodes at current level
            var currentLevel = new List<string>();
            int levelSize = queue.Count;
            
            for (int i = 0; i < levelSize; i++)
            {
                var current = queue.Dequeue();
                currentLevel.Add(current);
                processedCount++;
                
                // For each object that depends on current
                var dependents = graph.GetDependents(current);
                foreach (var dependent in dependents)
                {
                    inDegree[dependent]--;
                    
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
            
            levels.Add(currentLevel);
        }
        
        // If we didn't process all nodes, there's a cycle
        if (processedCount != allObjects.Count)
        {
            throw new InvalidOperationException(
                $"Cannot perform topological sort: graph contains circular dependencies. " +
                $"Processed {processedCount} of {allObjects.Count} objects.");
        }
        
        return levels;
    }
    
    /// <summary>
    /// Checks if the graph can be topologically sorted (i.e., is a DAG)
    /// </summary>
    public bool CanSort(DependencyGraph graph)
    {
        try
        {
            Sort(graph);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Calculates in-degree (number of dependencies) for each node
    /// </summary>
    private Dictionary<string, int> CalculateInDegree(DependencyGraph graph, List<string> allObjects)
    {
        var inDegree = new Dictionary<string, int>();
        
        // Initialize all nodes with in-degree 0
        foreach (var obj in allObjects)
        {
            inDegree[obj] = 0;
        }
        
        // Count incoming edges for each node
        foreach (var obj in allObjects)
        {
            var dependencies = graph.GetDependencies(obj);
            inDegree[obj] = dependencies.Count;
        }
        
        return inDegree;
    }
}
