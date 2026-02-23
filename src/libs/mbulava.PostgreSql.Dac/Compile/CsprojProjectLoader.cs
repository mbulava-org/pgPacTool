using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using System.Text.Json;
using System.Xml.Linq;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Loads PostgreSQL projects from SDK-style .csproj files.
/// Similar to MSBuild.Sdk.SqlProj for SQL Server.
/// Uses Npgquery parser for accurate SQL parsing.
/// </summary>
public class CsprojProjectLoader
{
    private readonly string _projectPath;
    private readonly string _projectDirectory;
    private readonly Parser _parser = new();

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
    /// Gets all SQL files in the project directory recursively.
    /// Convention: All .sql files are included automatically.
    /// Only Pre/Post deployment scripts need explicit configuration in .csproj.
    /// </summary>
    private List<string> GetSqlFilesFromProject(XDocument doc)
    {
        var sqlFiles = new List<string>();

        // Get pre/post deployment scripts to exclude them from main objects
        var prePostScripts = GetPrePostDeploymentScripts(doc);
        var excludeFiles = new HashSet<string>(prePostScripts.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);

        // Scan all .sql files recursively in project directory
        if (Directory.Exists(_projectDirectory))
        {
            var allSqlFiles = Directory.GetFiles(_projectDirectory, "*.sql", SearchOption.AllDirectories);

            foreach (var file in allSqlFiles)
            {
                var relativePath = Path.GetRelativePath(_projectDirectory, file);

                // Exclude bin, obj, and other build directories
                if (relativePath.Contains("bin") || 
                    relativePath.Contains("obj") || 
                    relativePath.Contains(".vs") ||
                    relativePath.StartsWith("."))
                {
                    continue;
                }

                // Exclude pre/post deployment scripts
                if (excludeFiles.Contains(relativePath))
                {
                    continue;
                }

                sqlFiles.Add(relativePath);
            }
        }

        return sqlFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Gets pre and post deployment scripts explicitly configured in .csproj.
    /// Format: &lt;PreDeploy Include="Scripts\PreDeploy.sql" /&gt;
    ///         &lt;PostDeploy Include="Scripts\PostDeploy.sql" /&gt;
    /// </summary>
    private List<(string FilePath, string Type)> GetPrePostDeploymentScripts(XDocument doc)
    {
        var scripts = new List<(string FilePath, string Type)>();

        // Look for PreDeploy elements
        var preDeployItems = doc.Descendants()
            .Where(e => e.Name.LocalName == "PreDeploy")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        foreach (var item in preDeployItems)
        {
            scripts.Add((item!, "PreDeploy"));
        }

        // Look for PostDeploy elements
        var postDeployItems = doc.Descendants()
            .Where(e => e.Name.LocalName == "PostDeploy")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        foreach (var item in postDeployItems)
        {
            scripts.Add((item!, "PostDeploy"));
        }

        return scripts;
    }

    /// <summary>
    /// Parses a SQL file and adds objects to the schema using Npgquery parser.
    /// </summary>
    private async Task ParseSqlFileAsync(string sql, string fileName, PgSchema schema)
    {
        try
        {
            // Parse with Npgquery
            var result = _parser.Parse(sql);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"Warning: Failed to parse {fileName}: {result.Error}");
                return;
            }

            if (result.ParseTree == null)
            {
                return;
            }

            // Process each statement in the parse tree
            var stmts = result.ParseTree.RootElement.GetProperty("stmts");

            foreach (var stmtWrapper in stmts.EnumerateArray())
            {
                if (!stmtWrapper.TryGetProperty("stmt", out var stmt))
                    continue;

                await ProcessStatementAsync(stmt, sql, fileName, schema);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error parsing {fileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes a single parsed statement and adds the appropriate object to the schema.
    /// </summary>
    private async Task ProcessStatementAsync(JsonElement stmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            // Check what type of statement this is
            if (stmt.TryGetProperty("CreateStmt", out var createStmt))
            {
                // This is a CREATE TABLE statement
                await ProcessCreateTableAsync(createStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("ViewStmt", out var viewStmt))
            {
                // This is a CREATE VIEW statement
                await ProcessCreateViewAsync(viewStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("CreateFunctionStmt", out var functionStmt))
            {
                // This is a CREATE FUNCTION statement
                await ProcessCreateFunctionAsync(functionStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("CompositeTypeStmt", out var compositeStmt))
            {
                // This is a CREATE TYPE (composite) statement
                await ProcessCreateCompositeTypeAsync(compositeStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("CreateEnumStmt", out var enumStmt))
            {
                // This is a CREATE TYPE AS ENUM statement
                await ProcessCreateEnumTypeAsync(enumStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("CreateDomainStmt", out var domainStmt))
            {
                // This is a CREATE DOMAIN statement
                await ProcessCreateDomainTypeAsync(domainStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("CreateSeqStmt", out var seqStmt))
            {
                // This is a CREATE SEQUENCE statement
                await ProcessCreateSequenceAsync(seqStmt, fullSql, fileName, schema);
            }
            else if (stmt.TryGetProperty("CreateTrigStmt", out var trigStmt))
            {
                // This is a CREATE TRIGGER statement
                await ProcessCreateTriggerAsync(trigStmt, fullSql, fileName, schema);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process statement in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateTableAsync(JsonElement createStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!createStmt.TryGetProperty("relation", out var relation))
                return;

            var tableName = GetRelationName(relation);
            if (string.IsNullOrEmpty(tableName))
                return;

            // Extract the CREATE TABLE statement from the full SQL
            var tableDefinition = ExtractStatementForObject(fullSql, "TABLE", tableName);

            var table = new PgTable
            {
                Name = tableName,
                Definition = tableDefinition ?? fullSql.Trim(),
                Owner = "postgres"
            };

            schema.Tables.Add(table);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE TABLE in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateViewAsync(JsonElement viewStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!viewStmt.TryGetProperty("view", out var viewRelation))
                return;

            var viewName = GetRelationName(viewRelation);
            if (string.IsNullOrEmpty(viewName))
                return;

            // Check if it's a materialized view
            var isMaterialized = viewStmt.TryGetProperty("replace", out _) ? false : 
                                 fullSql.ToUpperInvariant().Contains("MATERIALIZED VIEW");

            var viewDefinition = ExtractStatementForObject(fullSql, isMaterialized ? "MATERIALIZED VIEW" : "VIEW", viewName);

            var view = new PgView
            {
                Name = viewName,
                Definition = viewDefinition ?? fullSql.Trim(),
                Owner = "postgres",
                IsMaterialized = isMaterialized
            };

            schema.Views.Add(view);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE VIEW in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateFunctionAsync(JsonElement functionStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!functionStmt.TryGetProperty("funcname", out var funcnameArray))
                return;

            var functionName = GetQualifiedName(funcnameArray);
            if (string.IsNullOrEmpty(functionName))
                return;

            var functionDefinition = ExtractStatementForObject(fullSql, "FUNCTION", functionName);

            var function = new PgFunction
            {
                Name = functionName,
                Definition = functionDefinition ?? fullSql.Trim(),
                Owner = "postgres"
            };

            schema.Functions.Add(function);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE FUNCTION in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateCompositeTypeAsync(JsonElement compositeStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!compositeStmt.TryGetProperty("typevar", out var typevar))
                return;

            var typeName = GetRelationName(typevar);
            if (string.IsNullOrEmpty(typeName))
                return;

            var typeDefinition = ExtractStatementForObject(fullSql, "TYPE", typeName);

            var type = new PgType
            {
                Name = typeName,
                Definition = typeDefinition ?? fullSql.Trim(),
                Owner = "postgres",
                Kind = PgTypeKind.Composite
            };

            schema.Types.Add(type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE TYPE (composite) in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateEnumTypeAsync(JsonElement enumStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!enumStmt.TryGetProperty("typeName", out var typeNameArray))
                return;

            var typeName = GetQualifiedName(typeNameArray);
            if (string.IsNullOrEmpty(typeName))
                return;

            var typeDefinition = ExtractStatementForObject(fullSql, "TYPE", typeName);

            var type = new PgType
            {
                Name = typeName,
                Definition = typeDefinition ?? fullSql.Trim(),
                Owner = "postgres",
                Kind = PgTypeKind.Enum
            };

            schema.Types.Add(type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE TYPE (enum) in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateDomainTypeAsync(JsonElement domainStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!domainStmt.TryGetProperty("domainname", out var domainnameArray))
                return;

            var typeName = GetQualifiedName(domainnameArray);
            if (string.IsNullOrEmpty(typeName))
                return;

            var typeDefinition = ExtractStatementForObject(fullSql, "DOMAIN", typeName);

            var type = new PgType
            {
                Name = typeName,
                Definition = typeDefinition ?? fullSql.Trim(),
                Owner = "postgres",
                Kind = PgTypeKind.Domain
            };

            schema.Types.Add(type);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE DOMAIN in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateSequenceAsync(JsonElement seqStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!seqStmt.TryGetProperty("sequence", out var seqRelation))
                return;

            var sequenceName = GetRelationName(seqRelation);
            if (string.IsNullOrEmpty(sequenceName))
                return;

            var sequenceDefinition = ExtractStatementForObject(fullSql, "SEQUENCE", sequenceName);

            var sequence = new PgSequence
            {
                Name = sequenceName,
                Definition = sequenceDefinition ?? fullSql.Trim(),
                Owner = "postgres"
            };

            schema.Sequences.Add(sequence);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE SEQUENCE in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessCreateTriggerAsync(JsonElement trigStmt, string fullSql, string fileName, PgSchema schema)
    {
        try
        {
            if (!trigStmt.TryGetProperty("trigname", out var triggerNameElement))
                return;

            var triggerName = triggerNameElement.GetString();
            if (string.IsNullOrEmpty(triggerName))
                return;

            // Get the table name
            string tableName = "unknown";
            if (trigStmt.TryGetProperty("relation", out var relation))
            {
                tableName = GetRelationName(relation) ?? "unknown";
            }

            var triggerDefinition = ExtractStatementForObject(fullSql, "TRIGGER", triggerName);

            var trigger = new PgTrigger
            {
                Name = triggerName,
                TableName = tableName,
                Definition = triggerDefinition ?? fullSql.Trim(),
                Owner = "postgres"
            };

            schema.Triggers.Add(trigger);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to process CREATE TRIGGER in {fileName}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Extracts the name from a relation node (RangeVar).
    /// </summary>
    private static string? GetRelationName(JsonElement relation)
    {
        if (relation.TryGetProperty("relname", out var relnameElement))
        {
            return relnameElement.GetString();
        }
        return null;
    }

    /// <summary>
    /// Extracts a qualified name from an array of String nodes.
    /// </summary>
    private static string? GetQualifiedName(JsonElement nameArray)
    {
        var nameParts = new List<string>();

        foreach (var item in nameArray.EnumerateArray())
        {
            if (item.TryGetProperty("String", out var stringNode))
            {
                if (stringNode.TryGetProperty("sval", out var sval))
                {
                    var part = sval.GetString();
                    if (!string.IsNullOrEmpty(part))
                    {
                        nameParts.Add(part);
                    }
                }
            }
        }

        return nameParts.Count > 0 ? nameParts[^1] : null; // Return last part (unqualified name)
    }

    /// <summary>
    /// Extracts the specific CREATE statement for an object from the full SQL text.
    /// This handles files with multiple statements.
    /// </summary>
    private static string? ExtractStatementForObject(string fullSql, string objectType, string objectName)
    {
        // Simple extraction: find CREATE <type> <name> and extract until the next CREATE or end of file
        var pattern = $@"CREATE\s+(?:OR\s+REPLACE\s+)?{objectType}\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:\w+\.)?{objectName}";
        var match = System.Text.RegularExpressions.Regex.Match(fullSql, pattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        var startIndex = match.Index;

        // Find the end - look for next CREATE statement or end of string
        var nextCreatePattern = @"\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:TABLE|VIEW|FUNCTION|PROCEDURE|TYPE|SEQUENCE|TRIGGER|DOMAIN)";
        var nextMatch = System.Text.RegularExpressions.Regex.Match(fullSql.Substring(startIndex + match.Length), 
            nextCreatePattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        int endIndex;
        if (nextMatch.Success)
        {
            endIndex = startIndex + match.Length + nextMatch.Index;
        }
        else
        {
            endIndex = fullSql.Length;
        }

        return fullSql.Substring(startIndex, endIndex - startIndex).Trim();
    }
}
