using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Loads PostgreSQL projects from SDK-style .csproj files.
/// Similar to MSBuild.Sdk.SqlProj for SQL Server.
/// </summary>
public partial class CsprojProjectLoader
{
    private readonly string _projectPath;
    private readonly string _projectDirectory;

    public CsprojProjectLoader(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        _projectPath = projectPath;
        _projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Cannot determine project directory");
    }

    /// <summary>
    /// Loads a PgProject from a .csproj file.
    /// </summary>
    public async Task<PgProject> LoadProjectAsync()
    {
        var project = new PgProject
        {
            DatabaseName = Path.GetFileNameWithoutExtension(_projectPath)
        };

        // Parse .csproj XML
        var doc = XDocument.Load(_projectPath);
        
        // Get SQL files from the project
        var sqlFiles = GetSqlFilesFromProject(doc);

        // Parse SQL files and build schema
        var schema = new PgSchema
        {
            Name = "public", // Default schema, could be configurable
            Owner = "postgres"
        };

        foreach (var sqlFile in sqlFiles)
        {
            var fullPath = Path.Combine(_projectDirectory, sqlFile);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Warning: SQL file not found: {sqlFile}");
                continue;
            }

            var sql = await File.ReadAllTextAsync(fullPath);
            await ParseSqlFileAsync(sql, sqlFile, schema);
        }

        project.Schemas.Add(schema);
        return project;
    }

    /// <summary>
    /// Gets all SQL files referenced in the .csproj.
    /// </summary>
    private List<string> GetSqlFilesFromProject(XDocument doc)
    {
        var sqlFiles = new List<string>();

        // Look for <Content Include="**/*.sql" /> or similar patterns
        var contentItems = doc.Descendants()
            .Where(e => e.Name.LocalName == "Content" || e.Name.LocalName == "None" || e.Name.LocalName == "Compile")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v != null && v.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .ToList();

        sqlFiles.AddRange(contentItems!);

        // Also look for wildcards like **/*.sql
        var wildcardItems = doc.Descendants()
            .Where(e => e.Name.LocalName == "Content" || e.Name.LocalName == "None")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v != null && (v.Contains("*.sql") || v.Contains("**")))
            .ToList();

        foreach (var pattern in wildcardItems!)
        {
            var expandedFiles = ExpandWildcard(pattern);
            sqlFiles.AddRange(expandedFiles);
        }

        // If no explicit SQL files, scan directories
        if (sqlFiles.Count == 0)
        {
            // Default scan: look for common SQL directories
            var searchDirs = new[] { "Tables", "Views", "Functions", "Procedures", "Types", "Schemas", "Scripts", "SQL" };
            
            foreach (var dir in searchDirs)
            {
                var dirPath = Path.Combine(_projectDirectory, dir);
                if (Directory.Exists(dirPath))
                {
                    var files = Directory.GetFiles(dirPath, "*.sql", SearchOption.AllDirectories);
                    sqlFiles.AddRange(files.Select(f => Path.GetRelativePath(_projectDirectory, f)));
                }
            }

            // Also check root directory
            var rootFiles = Directory.GetFiles(_projectDirectory, "*.sql", SearchOption.TopDirectoryOnly);
            sqlFiles.AddRange(rootFiles.Select(f => Path.GetRelativePath(_projectDirectory, f)));
        }

        return sqlFiles.Distinct().ToList();
    }

    /// <summary>
    /// Expands wildcard patterns like **/*.sql
    /// </summary>
    private List<string> ExpandWildcard(string pattern)
    {
        var results = new List<string>();

        // Convert glob pattern to regex
        var searchOption = pattern.Contains("**") ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var searchPattern = pattern.Replace("**\\", "").Replace("**/", "");

        var searchDir = _projectDirectory;
        
        // Handle directory prefix in pattern
        var lastSlash = pattern.LastIndexOfAny(new[] { '\\', '/' });
        if (lastSlash > 0 && !pattern.Substring(0, lastSlash).Contains('*'))
        {
            var dirPrefix = pattern.Substring(0, lastSlash).Replace("**", "");
            searchDir = Path.Combine(_projectDirectory, dirPrefix);
            searchPattern = pattern.Substring(lastSlash + 1);
        }

        if (Directory.Exists(searchDir))
        {
            var files = Directory.GetFiles(searchDir, searchPattern, searchOption);
            results.AddRange(files.Select(f => Path.GetRelativePath(_projectDirectory, f)));
        }

        return results;
    }

    /// <summary>
    /// Parses a SQL file and adds objects to the schema.
    /// </summary>
    private async Task ParseSqlFileAsync(string sql, string fileName, PgSchema schema)
    {
        try
        {
            // Split by statement delimiter (;) and parse each statement
            var statements = SplitSqlStatements(sql);

            foreach (var statement in statements)
            {
                if (string.IsNullOrWhiteSpace(statement))
                    continue;

                await ParseStatementAsync(statement.Trim(), fileName, schema);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error parsing {fileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Splits SQL into individual statements.
    /// </summary>
    private static List<string> SplitSqlStatements(string sql)
    {
        var statements = new List<string>();
        var currentStatement = new System.Text.StringBuilder();
        var inString = false;
        var inComment = false;
        var inBlockComment = false;

        for (int i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            var next = i < sql.Length - 1 ? sql[i + 1] : '\0';

            // Handle comments
            if (!inString)
            {
                if (c == '-' && next == '-' && !inBlockComment)
                {
                    inComment = true;
                }
                else if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++; // Skip next char
                    continue;
                }
                else if (c == '*' && next == '/' && inBlockComment)
                {
                    inBlockComment = false;
                    i++; // Skip next char
                    continue;
                }
                else if (c == '\n' && inComment)
                {
                    inComment = false;
                }
            }

            // Handle strings
            if (c == '\'' && !inComment && !inBlockComment)
            {
                inString = !inString;
            }

            // Statement delimiter
            if (c == ';' && !inString && !inComment && !inBlockComment)
            {
                statements.Add(currentStatement.ToString());
                currentStatement.Clear();
                continue;
            }

            if (!inComment && !inBlockComment)
            {
                currentStatement.Append(c);
            }
        }

        // Add final statement if any
        if (currentStatement.Length > 0)
        {
            statements.Add(currentStatement.ToString());
        }

        return statements;
    }

    /// <summary>
    /// Parses a single SQL statement and adds the appropriate object to the schema.
    /// </summary>
    private static async Task ParseStatementAsync(string statement, string fileName, PgSchema schema)
    {
        var upperStatement = statement.TrimStart().ToUpperInvariant();

        try
        {
            if (upperStatement.StartsWith("CREATE TABLE"))
            {
                var table = ParseCreateTable(statement, fileName);
                if (table != null)
                {
                    schema.Tables.Add(table);
                }
            }
            else if (upperStatement.StartsWith("CREATE VIEW") || upperStatement.StartsWith("CREATE OR REPLACE VIEW"))
            {
                var view = ParseCreateView(statement, fileName);
                if (view != null)
                {
                    schema.Views.Add(view);
                }
            }
            else if (upperStatement.StartsWith("CREATE FUNCTION") || upperStatement.StartsWith("CREATE OR REPLACE FUNCTION"))
            {
                var function = ParseCreateFunction(statement, fileName);
                if (function != null)
                {
                    schema.Functions.Add(function);
                }
            }
            else if (upperStatement.StartsWith("CREATE TYPE"))
            {
                var type = ParseCreateType(statement, fileName);
                if (type != null)
                {
                    schema.Types.Add(type);
                }
            }
            else if (upperStatement.StartsWith("CREATE SEQUENCE"))
            {
                var sequence = ParseCreateSequence(statement, fileName);
                if (sequence != null)
                {
                    schema.Sequences.Add(sequence);
                }
            }
            else if (upperStatement.StartsWith("CREATE TRIGGER"))
            {
                var trigger = ParseCreateTrigger(statement, fileName);
                if (trigger != null)
                {
                    schema.Triggers.Add(trigger);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to parse statement in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    // Simple regex patterns for object name extraction
    [GeneratedRegex(@"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:(\w+)\.)?(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex TableNamePattern();

    [GeneratedRegex(@"CREATE\s+(?:OR\s+REPLACE\s+)?VIEW\s+(?:(\w+)\.)?(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex ViewNamePattern();

    [GeneratedRegex(@"CREATE\s+(?:OR\s+REPLACE\s+)?FUNCTION\s+(?:(\w+)\.)?(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionNamePattern();

    [GeneratedRegex(@"CREATE\s+TYPE\s+(?:(\w+)\.)?(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex TypeNamePattern();

    [GeneratedRegex(@"CREATE\s+SEQUENCE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:(\w+)\.)?(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex SequenceNamePattern();

    [GeneratedRegex(@"CREATE\s+TRIGGER\s+(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex TriggerNamePattern();

    private static PgTable? ParseCreateTable(string sql, string fileName)
    {
        var match = TableNamePattern().Match(sql);
        if (!match.Success) return null;

        var tableName = match.Groups[2].Value;
        
        return new PgTable
        {
            Name = tableName,
            Definition = sql.Trim(),
            Owner = "postgres"
        };
    }

    private static PgView? ParseCreateView(string sql, string fileName)
    {
        var match = ViewNamePattern().Match(sql);
        if (!match.Success) return null;

        var viewName = match.Groups[2].Value;

        return new PgView
        {
            Name = viewName,
            Definition = sql.Trim(),
            Owner = "postgres",
            IsMaterialized = sql.ToUpperInvariant().Contains("MATERIALIZED VIEW")
        };
    }

    private static PgFunction? ParseCreateFunction(string sql, string fileName)
    {
        var match = FunctionNamePattern().Match(sql);
        if (!match.Success) return null;

        var functionName = match.Groups[2].Value;

        return new PgFunction
        {
            Name = functionName,
            Definition = sql.Trim(),
            Owner = "postgres"
        };
    }

    private static PgType? ParseCreateType(string sql, string fileName)
    {
        var match = TypeNamePattern().Match(sql);
        if (!match.Success) return null;

        var typeName = match.Groups[2].Value;
        var kind = PgTypeKind.Domain; // Default

        if (sql.ToUpperInvariant().Contains("AS ENUM"))
            kind = PgTypeKind.Enum;
        else if (sql.ToUpperInvariant().Contains("AS (") || sql.ToUpperInvariant().Contains("AS\n("))
            kind = PgTypeKind.Composite;

        return new PgType
        {
            Name = typeName,
            Definition = sql.Trim(),
            Owner = "postgres",
            Kind = kind
        };
    }

    private static PgSequence? ParseCreateSequence(string sql, string fileName)
    {
        var match = SequenceNamePattern().Match(sql);
        if (!match.Success) return null;

        var sequenceName = match.Groups[2].Value;

        return new PgSequence
        {
            Name = sequenceName,
            Definition = sql.Trim(),
            Owner = "postgres"
        };
    }

    private static PgTrigger? ParseCreateTrigger(string sql, string fileName)
    {
        var match = TriggerNamePattern().Match(sql);
        if (!match.Success) return null;

        var triggerName = match.Groups[1].Value;

        // Try to extract table name
        var tableMatch = Regex.Match(sql, @"ON\s+(?:(\w+)\.)?(\w+)", RegexOptions.IgnoreCase);
        var tableName = tableMatch.Success ? tableMatch.Groups[2].Value : "unknown";

        return new PgTrigger
        {
            Name = triggerName,
            TableName = tableName,
            Definition = sql.Trim(),
            Owner = "postgres"
        };
    }
}
