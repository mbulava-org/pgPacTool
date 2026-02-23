using mbulava.PostgreSql.Dac.Models;
using System.Text.RegularExpressions;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Analyzes database objects to extract dependency information
/// </summary>
public class DependencyAnalyzer
{
    /// <summary>
    /// Analyzes a complete project and builds a dependency graph
    /// </summary>
    public DependencyGraph AnalyzeProject(PgProject project)
    {
        var graph = new DependencyGraph();
        
        foreach (var schema in project.Schemas)
        {
            // Add all objects to graph first
            foreach (var table in schema.Tables)
            {
                graph.AddObject($"{schema.Name}.{table.Name}", "TABLE");
            }
            
            foreach (var view in schema.Views)
            {
                graph.AddObject($"{schema.Name}.{view.Name}", "VIEW");
            }
            
            foreach (var function in schema.Functions)
            {
                graph.AddObject($"{schema.Name}.{function.Name}", "FUNCTION");
            }
            
            foreach (var trigger in schema.Triggers)
            {
                graph.AddObject($"{schema.Name}.{trigger.Name}", "TRIGGER");
            }
            
            foreach (var type in schema.Types)
            {
                graph.AddObject($"{schema.Name}.{type.Name}", "TYPE");
            }
            
            foreach (var sequence in schema.Sequences)
            {
                graph.AddObject($"{schema.Name}.{sequence.Name}", "SEQUENCE");
            }
            
            // Now extract and add dependencies
            foreach (var table in schema.Tables)
            {
                var deps = ExtractTableDependencies(schema.Name, table);
                foreach (var dep in deps)
                {
                    var fromName = $"{dep.ObjectSchema}.{dep.ObjectName}";
                    var toName = $"{dep.DependsOnSchema}.{dep.DependsOnName}";
                    graph.AddDependency(fromName, toName);
                }
            }
            
            foreach (var view in schema.Views)
            {
                var deps = ExtractViewDependencies(schema.Name, view);
                foreach (var dep in deps)
                {
                    var fromName = $"{dep.ObjectSchema}.{dep.ObjectName}";
                    var toName = $"{dep.DependsOnSchema}.{dep.DependsOnName}";
                    graph.AddDependency(fromName, toName);
                }
            }
            
            foreach (var function in schema.Functions)
            {
                var deps = ExtractFunctionDependencies(schema.Name, function);
                foreach (var dep in deps)
                {
                    var fromName = $"{dep.ObjectSchema}.{dep.ObjectName}";
                    var toName = $"{dep.DependsOnSchema}.{dep.DependsOnName}";
                    graph.AddDependency(fromName, toName);
                }
            }
            
            foreach (var trigger in schema.Triggers)
            {
                var deps = ExtractTriggerDependencies(schema.Name, trigger);
                foreach (var dep in deps)
                {
                    var fromName = $"{dep.ObjectSchema}.{dep.ObjectName}";
                    var toName = $"{dep.DependsOnSchema}.{dep.DependsOnName}";
                    graph.AddDependency(fromName, toName);
                }
            }
        }
        
        return graph;
    }
    
    /// <summary>
    /// Extracts dependencies from a table (foreign keys, inheritance, sequences)
    /// </summary>
    public List<PgDependency> ExtractTableDependencies(string schemaName, PgTable table)
    {
        var dependencies = new List<PgDependency>();
        
        // Extract foreign key dependencies
        foreach (var constraint in table.Constraints.Where(c => c.Type == PgQuery.ConstrType.ConstrForeign))
        {
            if (string.IsNullOrEmpty(constraint.ReferencedTable))
                continue;
            
            var (refSchema, refTable) = ParseQualifiedName(constraint.ReferencedTable, schemaName);
            
            dependencies.Add(new PgDependency
            {
                ObjectType = "TABLE",
                ObjectSchema = schemaName,
                ObjectName = table.Name,
                DependsOnType = "TABLE",
                DependsOnSchema = refSchema,
                DependsOnName = refTable,
                DependencyType = "FOREIGN_KEY"
            });
        }
        
        // Extract inheritance dependencies
        foreach (var parent in table.InheritedFrom)
        {
            var (parentSchema, parentTable) = ParseQualifiedName(parent, schemaName);
            
            dependencies.Add(new PgDependency
            {
                ObjectType = "TABLE",
                ObjectSchema = schemaName,
                ObjectName = table.Name,
                DependsOnType = "TABLE",
                DependsOnSchema = parentSchema,
                DependsOnName = parentTable,
                DependencyType = "INHERITANCE"
            });
        }
        
        // TODO: Extract sequence dependencies from DEFAULT nextval()
        // TODO: Extract type dependencies from columns
        
        return dependencies;
    }
    
    /// <summary>
    /// Extracts dependencies from a view (table/view references)
    /// </summary>
    public List<PgDependency> ExtractViewDependencies(string schemaName, PgView view)
    {
        var dependencies = new List<PgDependency>();
        
        // Use existing Dependencies list if available
        foreach (var dep in view.Dependencies)
        {
            var (depSchema, depName) = ParseQualifiedName(dep, schemaName);
            
            dependencies.Add(new PgDependency
            {
                ObjectType = "VIEW",
                ObjectSchema = schemaName,
                ObjectName = view.Name,
                DependsOnType = "TABLE_OR_VIEW",
                DependsOnSchema = depSchema,
                DependsOnName = depName,
                DependencyType = "VIEW_REFERENCE"
            });
        }
        
        // TODO: Parse view definition from AST for more comprehensive extraction
        
        return dependencies;
    }
    
    /// <summary>
    /// Extracts dependencies from a function (types, tables, other functions)
    /// </summary>
    public List<PgDependency> ExtractFunctionDependencies(string schemaName, PgFunction function)
    {
        var dependencies = new List<PgDependency>();
        
        // For MVP: Simple text-based extraction
        // TODO: Parse function AST for comprehensive extraction
        
        // Look for table references in function body (basic pattern matching)
        if (!string.IsNullOrEmpty(function.Definition))
        {
            var tablePattern = @"FROM\s+([a-zA-Z_][a-zA-Z0-9_]*\.)?([a-zA-Z_][a-zA-Z0-9_]*)";
            var matches = Regex.Matches(function.Definition, tablePattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var schema = match.Groups[1].Success 
                    ? match.Groups[1].Value.TrimEnd('.') 
                    : schemaName;
                var tableName = match.Groups[2].Value;
                
                // Skip common keywords
                if (IsKeyword(tableName))
                    continue;
                
                dependencies.Add(new PgDependency
                {
                    ObjectType = "FUNCTION",
                    ObjectSchema = schemaName,
                    ObjectName = function.Name,
                    DependsOnType = "TABLE",
                    DependsOnSchema = schema,
                    DependsOnName = tableName,
                    DependencyType = "FUNCTION_REFERENCE"
                });
            }
        }
        
        return dependencies;
    }
    
    /// <summary>
    /// Extracts dependencies from a trigger (table and function)
    /// </summary>
    public List<PgDependency> ExtractTriggerDependencies(string schemaName, PgTrigger trigger)
    {
        var dependencies = new List<PgDependency>();
        
        // Trigger always depends on its table
        var (tableSchema, tableName) = ParseQualifiedName(trigger.TableName, schemaName);
        dependencies.Add(new PgDependency
        {
            ObjectType = "TRIGGER",
            ObjectSchema = schemaName,
            ObjectName = trigger.Name,
            DependsOnType = "TABLE",
            DependsOnSchema = tableSchema,
            DependsOnName = tableName,
            DependencyType = "TRIGGER_TABLE"
        });
        
        // Extract function name from trigger definition
        if (!string.IsNullOrEmpty(trigger.Definition))
        {
            // Pattern: EXECUTE FUNCTION schema.function_name() or EXECUTE PROCEDURE
            var funcPattern = @"EXECUTE\s+(FUNCTION|PROCEDURE)\s+(?:([a-zA-Z_][a-zA-Z0-9_]*)\.)?([a-zA-Z_][a-zA-Z0-9_]*)\s*\(";
            var match = Regex.Match(trigger.Definition, funcPattern, RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                var funcSchema = match.Groups[2].Success 
                    ? match.Groups[2].Value 
                    : schemaName;
                var funcName = match.Groups[3].Value;
                
                dependencies.Add(new PgDependency
                {
                    ObjectType = "TRIGGER",
                    ObjectSchema = schemaName,
                    ObjectName = trigger.Name,
                    DependsOnType = "FUNCTION",
                    DependsOnSchema = funcSchema,
                    DependsOnName = funcName,
                    DependencyType = "TRIGGER_FUNCTION"
                });
            }
        }
        
        return dependencies;
    }
    
    /// <summary>
    /// Parses a potentially qualified name (schema.object) into parts
    /// </summary>
    private (string schema, string name) ParseQualifiedName(string qualifiedName, string defaultSchema)
    {
        if (string.IsNullOrEmpty(qualifiedName))
            return (defaultSchema, string.Empty);
        
        var parts = qualifiedName.Split('.');
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        
        return (defaultSchema, qualifiedName);
    }
    
    /// <summary>
    /// Checks if a word is a SQL keyword
    /// </summary>
    private bool IsKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "OUTER", "LEFT", "RIGHT",
            "ON", "AS", "AND", "OR", "NOT", "NULL", "TRUE", "FALSE", "VALUES"
        };
        
        return keywords.Contains(word);
    }
}
