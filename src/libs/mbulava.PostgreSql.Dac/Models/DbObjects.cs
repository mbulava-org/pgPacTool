using PgQuery;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace mbulava.PostgreSql.Dac.Models
{
    public class PgProject
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string PostgresVersion { get; set; } = string.Empty;
        public string SourceConnection { get; set; } = string.Empty; // Sanitized connection string for documentation
        public string DefaultOwner { get; set; } = "postgres";
        public string DefaultTablespace { get; set; } = "pg_default";

        public List<PgSchema> Schemas { get; set; } = new();
        public List<PgRole> Roles { get; set; } = new();   

        public static async Task Save(PgProject project, Stream output)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            await JsonSerializer.SerializeAsync(output, project, options);
        }

        public static async Task<PgProject> Load(Stream input)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            return await JsonSerializer.DeserializeAsync<PgProject>(input, options) 
                ?? throw new InvalidOperationException("Failed to deserialize PgProject");
        }
    }

    public class PgSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;

        // SQL definition from database
        public string Definition { get; set; } = string.Empty;

        // Parsed AST for programmatic access and comparison
        public CreateSchemaStmt Ast { get; set; }
        public List<PgPrivilege> Privileges { get; set; } = new();

        public List<PgTable> Tables { get; set; } = new();
        public List<PgView> Views { get; set; } = new();
        public List<PgFunction> Functions { get; set; } = new();
        public List<PgType> Types { get; set; } = new();
        public List<PgSequence> Sequences { get; set; } = new();
        public List<PgTrigger> Triggers { get; set; } = new();
    }

    public class PgTable
    {
        public string Name { get; set; } = string.Empty;

        // SQL definition from database
        public string Definition { get; set; } = string.Empty;

        // Parsed AST for programmatic access and comparison
        public CreateStmt? Ast { get; set; }

        public string Owner { get; set; } = string.Empty;
        public string? Tablespace { get; set; }  // Tablespace name
        public bool RowLevelSecurity { get; set; }  // RLS enabled
        public bool ForceRowLevelSecurity { get; set; }  // RLS forced for table owner
        public int? FillFactor { get; set; }  // Storage fill factor
        public List<string> InheritedFrom { get; set; } = new();  // Parent tables
        public string? PartitionStrategy { get; set; }  // RANGE, LIST, HASH, or null
        public string? PartitionExpression { get; set; }  // Partition key expression

        public List<PgColumn> Columns { get; set; } = new();
        public List<PgConstraint> Constraints { get; set; } = new();
        public List<PgIndex> Indexes { get; set; } = new();
        public List<PgPrivilege> Privileges { get; set; } = new();

        // Relationship helpers
        public List<PgConstraint> ForeignKeys => Constraints.Where(c => c.Type == ConstrType.ConstrForeign).ToList();
        public List<PgConstraint> CheckConstraints => Constraints.Where(c => c.Type == ConstrType.ConstrCheck).ToList();
        public List<PgConstraint> UniqueConstraints => Constraints.Where(c => c.Type == ConstrType.ConstrUnique).ToList();
        public PgConstraint? PrimaryKey => Constraints.FirstOrDefault(c => c.Type == ConstrType.ConstrPrimary);
    }

    public class PgColumn
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNotNull { get; set; }
        public string? DefaultExpression { get; set; }
        public int Position { get; set; }  // Ordinal position
        public bool IsIdentity { get; set; }  // IDENTITY column
        public string? IdentityGeneration { get; set; }  // ALWAYS or BY DEFAULT
        public bool IsGenerated { get; set; }  // Generated/computed column
        public string? GenerationExpression { get; set; }  // Expression for generated columns
        public string? Collation { get; set; }  // Column collation
        public string? Comment { get; set; }  // Column comment
    }

    public class PgConstraint
    {
        public string Name { get; set; }
        public string Definition { get; set; } = string.Empty;
        public ConstrType Type { get; set; }
        public List<string> Keys { get; set; } = new();
        public string? CheckExpression { get; set; }
        public string? ReferencedTable { get; set; }
        public List<string>? ReferencedColumns { get; set; }
    }

    public class PgIndex
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    public class PgView
    {
        public string Name { get; set; } = string.Empty;

        // SQL definition from database (CREATE VIEW statement)
        public string Definition { get; set; } = string.Empty;

        // Parsed AST for programmatic access and comparison
        public ViewStmt? Ast { get; set; }

        public string Owner { get; set; } = string.Empty;
        public bool IsMaterialized { get; set; }  // true = materialized view, false = regular view
        public List<PgPrivilege> Privileges { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();  // Referenced tables/views
    }

    public class PgFunction
    {
        public string Name { get; set; } = string.Empty;

        // SQL definition from database (CREATE FUNCTION/PROCEDURE statement)
        public string Definition { get; set; } = string.Empty;

        // Parsed AST for programmatic access and comparison
        public CreateFunctionStmt? Ast { get; set; }

        public string Owner { get; set; } = string.Empty;
        public List<PgPrivilege> Privileges { get; set; } = new();
    }

    public enum PgTypeKind
    {
        Domain,
        Enum,
        Composite
    }

    public class PgType
    {
        public string Name { get; set; }
        public PgTypeKind Kind { get; set; }

        // SQL definition from database (CREATE TYPE statement)
        public string Definition { get; set; }

        public string Owner { get; set; } = string.Empty;

        // Parsed AST - type depends on Kind
        public CreateDomainStmt? AstDomain { get; set; }      // When Kind == Domain
        public CreateEnumStmt? AstEnum { get; set; }          // When Kind == Enum
        public CompositeTypeStmt? AstComposite { get; set; }  // When Kind == Composite

        // Extra metadata for convenience
        public List<string>? EnumLabels { get; set; }                  // For enums
        public List<PgAttribute>? CompositeAttributes { get; set; }    // For composites

        public List<PgPrivilege> Privileges { get; set; } = new();
    }

    public class PgAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
    }

    public class SeqOption
    {
        public string OptionName { get; set; } = string.Empty;   // e.g. "START", "INCREMENT", "CACHE", "CYCLE"
        public string OptionValue { get; set; } = string.Empty;  // e.g. "1", "5", "10", "true"
    }


    public class PgSequence
    {
        public string Name { get; set; }

        // SQL definition from database (CREATE SEQUENCE statement)
        public string Definition { get; set; }

        public string Owner { get; set; } = string.Empty;

        // Parsed AST for programmatic access and comparison
        public CreateSeqStmt? Ast { get; set; }

        public List<SeqOption> Options { get; set; } = new();
        public List<PgPrivilege> Privileges { get; set; } = new();
    }

    public class PgTrigger
    {
        public string Name { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;

        // SQL definition from database (CREATE TRIGGER statement)
        public string Definition { get; set; } = string.Empty;

        // Parsed AST for programmatic access and comparison
        public CreateTrigStmt? Ast { get; set; }

        public string Owner { get; set; } = string.Empty;
    }

    public class PgRole
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSuperUser { get; set; }
        public bool CanLogin { get; set; }
        public bool Inherit { get; set; }
        public bool Replication { get; set; }
        public bool BypassRLS { get; set; }
        public string? Password { get; set; }  // Optional, usually null for security
        public List<string> MemberOf { get; set; } = new();  // Role memberships

        // SQL to recreate role (CREATE ROLE statement)
        public string Definition { get; set; } = string.Empty;
    }

    public class PgPrivilege
    {
        public string Grantee { get; set; } = string.Empty;     // Role or PUBLIC
        public string PrivilegeType { get; set; } = string.Empty; // SELECT, INSERT, USAGE, CREATE, etc.
        public bool IsGrantable { get; set; }                   // WITH GRANT OPTION
        public string Grantor { get; set; } = string.Empty;     // Who granted it
    }

    /// <summary>
    /// Represents a dependency between database objects
    /// </summary>
    public class PgDependency
    {
        public string ObjectType { get; set; } = string.Empty;  // TABLE, VIEW, FUNCTION, etc.
        public string ObjectSchema { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string DependsOnType { get; set; } = string.Empty;
        public string DependsOnSchema { get; set; } = string.Empty;
        public string DependsOnName { get; set; } = string.Empty;
        public string DependencyType { get; set; } = string.Empty;  // NORMAL, AUTO, INTERNAL, etc.

        public string QualifiedObjectName => $"{ObjectSchema}.{ObjectName}";
        public string QualifiedDependsOnName => $"{DependsOnSchema}.{DependsOnName}";
    }

    /// <summary>
    /// Dependency graph for topological sorting and cycle detection
    /// </summary>
    public class DependencyGraph
    {
        private readonly Dictionary<string, HashSet<string>> _dependencies = new();
        private readonly Dictionary<string, string> _objectTypes = new();

        public void AddObject(string qualifiedName, string objectType)
        {
            if (!_dependencies.ContainsKey(qualifiedName))
            {
                _dependencies[qualifiedName] = new HashSet<string>();
            }
            _objectTypes[qualifiedName] = objectType;
        }

        public void AddDependency(string from, string to)
        {
            if (!_dependencies.ContainsKey(from))
            {
                _dependencies[from] = new HashSet<string>();
            }
            _dependencies[from].Add(to);
        }

        public List<string> TopologicalSort()
        {
            var sorted = new List<string>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var node in _dependencies.Keys)
            {
                if (!visited.Contains(node))
                {
                    Visit(node, visited, visiting, sorted);
                }
            }

            // Don't reverse - we want dependencies first, then dependents
            return sorted;
        }

        private void Visit(string node, HashSet<string> visited, HashSet<string> visiting, List<string> sorted)
        {
            if (visiting.Contains(node))
            {
                throw new InvalidOperationException($"Circular dependency detected involving: {node}");
            }

            if (!visited.Contains(node))
            {
                visiting.Add(node);

                if (_dependencies.ContainsKey(node))
                {
                    foreach (var dependency in _dependencies[node])
                    {
                        Visit(dependency, visited, visiting, sorted);
                    }
                }

                visiting.Remove(node);
                visited.Add(node);
                sorted.Add(node);
            }
        }

        public List<List<string>> DetectCycles()
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var recursionStack = new Stack<string>();

            foreach (var node in _dependencies.Keys)
            {
                if (!visited.Contains(node))
                {
                    DetectCyclesUtil(node, visited, recursionStack, cycles);
                }
            }

            return cycles;
        }

        private bool DetectCyclesUtil(string node, HashSet<string> visited, Stack<string> recursionStack, List<List<string>> cycles)
        {
            visited.Add(node);
            recursionStack.Push(node);

            if (_dependencies.ContainsKey(node))
            {
                foreach (var neighbor in _dependencies[node])
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (DetectCyclesUtil(neighbor, visited, recursionStack, cycles))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        // Found a cycle
                        var cycle = new List<string>();
                        var cycleStart = false;
                        foreach (var item in recursionStack.Reverse())
                        {
                            if (item == neighbor) cycleStart = true;
                            if (cycleStart) cycle.Add(item);
                        }
                        cycle.Add(neighbor); // Complete the cycle
                        cycles.Add(cycle);
                        return true;
                    }
                }
            }

            recursionStack.Pop();
            return false;
        }

        /// <summary>
        /// Gets all direct dependencies of an object
        /// </summary>
        public List<string> GetDependencies(string objectName)
        {
            if (!_dependencies.ContainsKey(objectName))
                return new List<string>();

            return _dependencies[objectName].ToList();
        }

        /// <summary>
        /// Gets all objects that depend on this object (reverse dependencies)
        /// </summary>
        public List<string> GetDependents(string objectName)
        {
            var dependents = new List<string>();

            foreach (var kvp in _dependencies)
            {
                if (kvp.Value.Contains(objectName))
                {
                    dependents.Add(kvp.Key);
                }
            }

            return dependents;
        }

        /// <summary>
        /// Checks if there is a path from one object to another
        /// </summary>
        public bool HasPath(string from, string to)
        {
            // Self-reference is always true
            if (from == to)
                return true;

            if (!_dependencies.ContainsKey(from))
                return false;

            var visited = new HashSet<string>();
            return HasPathDFS(from, to, visited);
        }

        private bool HasPathDFS(string current, string target, HashSet<string> visited)
        {
            if (current == target)
                return true;

            if (visited.Contains(current))
                return false;

            visited.Add(current);

            if (!_dependencies.ContainsKey(current))
                return false;

            foreach (var neighbor in _dependencies[current])
            {
                if (HasPathDFS(neighbor, target, visited))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all paths from one object to another
        /// </summary>
        public List<List<string>> GetAllPaths(string from, string to)
        {
            var allPaths = new List<List<string>>();
            var currentPath = new List<string>();
            var visited = new HashSet<string>();

            FindAllPathsDFS(from, to, visited, currentPath, allPaths);

            return allPaths;
        }

        private void FindAllPathsDFS(string current, string target, HashSet<string> visited, 
            List<string> currentPath, List<List<string>> allPaths)
        {
            visited.Add(current);
            currentPath.Add(current);

            if (current == target)
            {
                // Found a path, add a copy to results
                allPaths.Add(new List<string>(currentPath));
            }
            else if (_dependencies.ContainsKey(current))
            {
                foreach (var neighbor in _dependencies[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        FindAllPathsDFS(neighbor, target, visited, currentPath, allPaths);
                    }
                }
            }

            // Backtrack
            currentPath.RemoveAt(currentPath.Count - 1);
            visited.Remove(current);
        }

        /// <summary>
        /// Gets the type of an object
        /// </summary>
        public string? GetObjectType(string objectName)
        {
            return _objectTypes.TryGetValue(objectName, out var type) ? type : null;
        }

        /// <summary>
        /// Gets all objects in the graph
        /// </summary>
        public List<string> GetAllObjects()
        {
            return _dependencies.Keys.ToList();
        }

        public Dictionary<string, HashSet<string>> GetDependencies() => _dependencies;
    }
}
