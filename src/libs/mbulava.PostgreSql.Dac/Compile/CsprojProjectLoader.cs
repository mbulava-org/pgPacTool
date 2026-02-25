using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using PgQuery;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Output format for compilation.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// PostgreSQL Data-tier Application Package (.pgpac) - ZIP file containing content.json.
    /// Similar to SQL Server's .dacpac format.
    /// </summary>
    DacPac,

    /// <summary>
    /// Plain JSON file (.pgproj.json).
    /// </summary>
    Json
}

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

        // Get database name from project properties if specified
        var dbNameElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DatabaseName");
        if (dbNameElement != null && !string.IsNullOrWhiteSpace(dbNameElement.Value))
        {
            project.DatabaseName = dbNameElement.Value;
        }

        // Get PostgreSQL version from project properties if specified
        var pgVersionElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "PostgresVersion");
        if (pgVersionElement != null && !string.IsNullOrWhiteSpace(pgVersionElement.Value))
        {
            project.PostgresVersion = pgVersionElement.Value;
        }

        // Get default owner from project properties
        var defaultOwnerElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DefaultOwner");
        if (defaultOwnerElement != null && !string.IsNullOrWhiteSpace(defaultOwnerElement.Value))
        {
            project.DefaultOwner = defaultOwnerElement.Value;
        }

        // Get default tablespace from project properties
        var defaultTablespaceElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DefaultTablespace");
        if (defaultTablespaceElement != null && !string.IsNullOrWhiteSpace(defaultTablespaceElement.Value))
        {
            project.DefaultTablespace = defaultTablespaceElement.Value;
        }

        // Get SQL files from the project (convention-based: all *.sql files)
        var sqlFiles = GetSqlFilesFromProject(doc);

        // Phase 1: Parse all SQL files and extract object information from AST
        var parsedObjects = new List<ParsedSqlObject>();
        foreach (var sqlFile in sqlFiles)
        {
            var fullPath = Path.Combine(_projectDirectory, sqlFile);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Warning: SQL file not found: {sqlFile}");
                continue;
            }

            var sql = await File.ReadAllTextAsync(fullPath);
            var parsed = await ParseAndClassifySqlFileAsync(sql, sqlFile);
            if (parsed != null)
            {
                parsedObjects.Add(parsed);
            }
        }

        // Phase 2: Build dependency graph and order objects
        var orderedObjects = OrderObjectsByDependencies(parsedObjects);

        // Phase 3: Group by schema (extracted from AST, not folder structure)
        var schemaGroups = orderedObjects.GroupBy(o => o.SchemaName);

        foreach (var schemaGroup in schemaGroups)
        {
            var schema = new PgSchema
            {
                Name = schemaGroup.Key,
                Owner = project.DefaultOwner // Use default from project settings
            };

            // Process objects in dependency order
            foreach (var obj in schemaGroup)
            {
                await AddObjectToSchemaAsync(schema, obj);
            }

            project.Schemas.Add(schema);
        }

        return project;
    }

    /// <summary>
    /// Parses a SQL file and extracts object information from AST.
    /// </summary>
    private async Task<ParsedSqlObject?> ParseAndClassifySqlFileAsync(string sql, string filePath)
    {
        try
        {
            var result = _parser.Parse(sql);
            if (!result.IsSuccess || result.ParseTree == null)
            {
                Console.WriteLine($"Warning: Failed to parse {filePath}: {result.Error}");
                return null;
            }

            var astJson = result.ParseTree.RootElement.GetRawText();
            var parsed = new ParsedSqlObject
            {
                Sql = sql,
                FilePath = filePath,
                AstJson = astJson
            };

            // Determine object type and extract schema/name from AST
            if (sql.Contains("CREATE SCHEMA", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CreateSchemaStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Schema;
                parsed.SchemaName = stmt?.Schemaname ?? "public";
                parsed.ObjectName = stmt?.Schemaname ?? "public";
            }
            else if (sql.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CreateStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Table;
                ExtractSchemaAndName(stmt?.Relation, out var schema, out var name);
                parsed.SchemaName = schema ?? "public";
                parsed.ObjectName = name ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase) || 
                     sql.Contains("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<IndexStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Index;
                ExtractSchemaAndName(stmt?.Relation, out var schema, out var name);
                parsed.SchemaName = schema ?? "public";
                parsed.ObjectName = stmt?.Idxname ?? name ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("CREATE VIEW", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<ViewStmt>(astJson);
                parsed.ObjectType = SqlObjectType.View;
                ExtractSchemaAndName(stmt?.View, out var schema, out var name);
                parsed.SchemaName = schema ?? "public";
                parsed.ObjectName = name ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("CREATE FUNCTION", StringComparison.OrdinalIgnoreCase) ||
                     sql.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CreateFunctionStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Function;
                // Function name is in Funcname array
                if (stmt?.Funcname != null && stmt.Funcname.Any())
                {
                    var nameNode = stmt.Funcname.Last();
                    parsed.ObjectName = nameNode?.String?.Sval ?? "unknown";
                    if (stmt.Funcname.Count > 1)
                    {
                        parsed.SchemaName = stmt.Funcname[0]?.String?.Sval ?? "public";
                    }
                    else
                    {
                        parsed.SchemaName = "public";
                    }
                }
            }
            else if (sql.Contains("CREATE TYPE", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CompositeTypeStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Type;
                ExtractSchemaAndName(stmt?.Typevar, out var schema, out var name);
                parsed.SchemaName = schema ?? "public";
                parsed.ObjectName = name ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("CREATE SEQUENCE", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CreateSeqStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Sequence;
                ExtractSchemaAndName(stmt?.Sequence, out var schema, out var name);
                parsed.SchemaName = schema ?? "public";
                parsed.ObjectName = name ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("CREATE TRIGGER", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CreateTrigStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Trigger;
                ExtractSchemaAndName(stmt?.Relation, out var schema, out var name);
                parsed.SchemaName = schema ?? "public";
                parsed.ObjectName = stmt?.Trigname ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("CREATE ROLE", StringComparison.OrdinalIgnoreCase))
            {
                var stmt = JsonSerializer.Deserialize<CreateRoleStmt>(astJson);
                parsed.ObjectType = SqlObjectType.Role;
                parsed.SchemaName = ""; // Roles are not schema-scoped
                parsed.ObjectName = stmt?.Role ?? "unknown";
                parsed.Ast = stmt;
            }
            else if (sql.Contains("GRANT", StringComparison.OrdinalIgnoreCase))
            {
                parsed.ObjectType = SqlObjectType.Permission;
                parsed.SchemaName = "public"; // Will be refined later
                parsed.ObjectName = "_permissions";
            }
            else if (sql.Contains("ALTER", StringComparison.OrdinalIgnoreCase) && sql.Contains("OWNER", StringComparison.OrdinalIgnoreCase))
            {
                parsed.ObjectType = SqlObjectType.Owner;
                parsed.SchemaName = "public"; // Will be refined later
                parsed.ObjectName = "_owners";
            }
            else if (sql.Contains("COMMENT ON", StringComparison.OrdinalIgnoreCase))
            {
                parsed.ObjectType = SqlObjectType.Comment;
                parsed.SchemaName = "public"; // Will be refined later
                parsed.ObjectName = "_comments";
            }
            else
            {
                Console.WriteLine($"Warning: Unknown object type in {filePath}");
                return null;
            }

            return parsed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts schema and object name from RangeVar.
    /// </summary>
    private void ExtractSchemaAndName(RangeVar? rangeVar, out string? schema, out string? name)
    {
        schema = rangeVar?.Schemaname;
        name = rangeVar?.Relname;
    }

    /// <summary>
    /// Orders objects by dependencies (schemas first, then types, sequences, tables, etc.)
    /// </summary>
    private List<ParsedSqlObject> OrderObjectsByDependencies(List<ParsedSqlObject> objects)
    {
        var ordered = new List<ParsedSqlObject>();

        // Phase 1: Schemas (must be created first)
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Schema));

        // Phase 2: Roles (needed for ownership)
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Role));

        // Phase 3: Types (needed before tables that reference them)
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Type));

        // Phase 4: Sequences (needed before tables with DEFAULT nextval)
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Sequence));

        // Phase 5: Tables
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Table));

        // Phase 6: Indexes
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Index));

        // Phase 7: Views
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.View));

        // Phase 8: Functions
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Function));

        // Phase 9: Triggers
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Trigger));

        // Phase 10: Ownership changes
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Owner));

        // Phase 11: Permissions
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Permission));

        // Phase 12: Comments
        ordered.AddRange(objects.Where(o => o.ObjectType == SqlObjectType.Comment));

        return ordered;
    }

    /// <summary>
    /// Adds a parsed object to the schema.
    /// </summary>
    private async Task AddObjectToSchemaAsync(PgSchema schema, ParsedSqlObject obj)
    {
        // For now, just parse and add - full implementation would use AST
        await ParseSqlFileAsync(obj.Sql, obj.FilePath, schema);
    }

    /// <summary>
    /// Compiles the project and generates output based on format.
    /// </summary>
    /// <param name="outputPath">Output file path. If null, uses default based on format.</param>
    /// <param name="format">Output format (DacPac or Json).</param>
    /// <returns>Path to the generated output file.</returns>
    public async Task<string> CompileAndGenerateOutputAsync(string? outputPath = null, OutputFormat format = OutputFormat.DacPac)
    {
        // Load the project
        var project = await LoadProjectAsync();

        // Always save the full pgproj.json to obj folder for debugging/inspection
        var objDir = Path.Combine(_projectDirectory, "obj");
        Directory.CreateDirectory(objDir);
        var objJsonPath = Path.Combine(objDir, $"{project.DatabaseName}.pgproj.json");
        await GenerateJsonAsync(project, objJsonPath);

        // Determine output path
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var outputDir = Path.Combine(_projectDirectory, "bin", "Debug", "net10.0");
            Directory.CreateDirectory(outputDir);

            outputPath = format switch
            {
                OutputFormat.DacPac => Path.Combine(outputDir, $"{project.DatabaseName}.pgpac"),
                OutputFormat.Json => Path.Combine(outputDir, $"{project.DatabaseName}.pgproj.json"),
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        // Generate output
        switch (format)
        {
            case OutputFormat.DacPac:
                await GeneratePgPacAsync(project, outputPath);
                break;

            case OutputFormat.Json:
                await GenerateJsonAsync(project, outputPath);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }

        return outputPath;
    }

    /// <summary>
    /// Generates a .pgpac file (ZIP containing content.json).
    /// </summary>
    private static async Task GeneratePgPacAsync(PgProject project, string outputPath)
    {
        // Delete existing file if present
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        // Create ZIP file
        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

        // Add content.json entry
        var contentEntry = archive.CreateEntry("content.json", CompressionLevel.Optimal);

        await using var entryStream = contentEntry.Open();
        await PgProject.Save(project, entryStream);
    }

    /// <summary>
    /// Generates a plain .pgproj.json file.
    /// </summary>
    private static async Task GenerateJsonAsync(PgProject project, string outputPath)
    {
        await using var fileStream = File.Create(outputPath);
        await PgProject.Save(project, fileStream);
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

/// <summary>
/// SQL object type classification.
/// </summary>
internal enum SqlObjectType
{
    Schema,
    Table,
    Index,
    View,
    Function,
    Type,
    Sequence,
    Trigger,
    Role,
    Permission,
    Owner,
    Comment
}

/// <summary>
/// Parsed SQL object with AST information.
/// </summary>
internal class ParsedSqlObject
{
    public string Sql { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string AstJson { get; set; } = string.Empty;
    public SqlObjectType ObjectType { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public object? Ast { get; set; }
    public List<string> Dependencies { get; set; } = new();
}
