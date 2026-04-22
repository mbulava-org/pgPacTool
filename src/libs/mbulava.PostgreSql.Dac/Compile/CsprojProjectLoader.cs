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
    private PgProject? _currentProject;

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
        _currentProject = project;

        // Parse .csproj XML
        var doc = XDocument.Load(_projectPath);

        // Get database name from project properties if specified
        var dbNameElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DatabaseName");
        if (dbNameElement != null && !string.IsNullOrWhiteSpace(dbNameElement.Value))
        {
            project.DatabaseName = dbNameElement.Value;
        }

        project.PostgresVersion = GetRequiredPostgresVersion(doc);
        project.DefaultSchema = GetDefaultSchema(doc);

        // Get default owner from project properties (empty string means "not configured")
        var defaultOwnerElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DefaultOwner");
        project.DefaultOwner = (defaultOwnerElement != null && !string.IsNullOrWhiteSpace(defaultOwnerElement.Value))
            ? defaultOwnerElement.Value
            : string.Empty;

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
        using var parser = CreateParser(project.PostgresVersion);
        foreach (var sqlFile in sqlFiles)
        {
            var fullPath = Path.Combine(_projectDirectory, sqlFile);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Warning: SQL file not found: {sqlFile}");
                continue;
            }

            var sql = await File.ReadAllTextAsync(fullPath);
            var parsed = await ParseAndClassifySqlFileAsync(parser, project.DefaultSchema, sql, sqlFile);
            if (parsed != null)
            {
                parsedObjects.Add(parsed);
            }
        }

        // Phase 2: Build dependency graph and order objects
        var orderedObjects = OrderObjectsByDependencies(parsedObjects);

        // Separate roles from schema-scoped objects (roles are database-level)
        var roles = orderedObjects.Where(o => o.ObjectType == SqlObjectType.Role).ToList();
        var schemaObjects = orderedObjects.Where(o => o.ObjectType != SqlObjectType.Role).ToList();

        // Phase 3: Group by schema (extracted from AST, not folder structure)
        var schemaGroups = schemaObjects.GroupBy(o => o.SchemaName ?? project.DefaultSchema);

        foreach (var schemaGroup in schemaGroups)
        {
            // Skip empty schema names (defensive)
            if (string.IsNullOrWhiteSpace(schemaGroup.Key))
            {
                Console.WriteLine("Warning: Skipping objects with empty schema name");
                continue;
            }

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

        // Phase 4: Add roles to project (roles are database-level, not schema-scoped)
        foreach (var roleObj in roles)
        {
            if (!string.IsNullOrWhiteSpace(roleObj.ObjectName))
            {
                project.Roles.Add(new PgRole
                {
                    Name = roleObj.ObjectName,
                    Definition = roleObj.Sql
                });
            }
        }

        _currentProject = null;
        return project;
    }

    /// <summary>
    /// Parses a SQL file and extracts object information from AST.
    /// </summary>
    private async Task<ParsedSqlObject?> ParseAndClassifySqlFileAsync(Parser parser, string defaultSchema, string sql, string filePath)
    {
        try
        {
            var result = parser.Parse(sql);
            if (!result.IsSuccess || result.ParseTree == null)
            {
                Console.WriteLine($"Warning: Failed to parse {filePath}: {result.Error}");
                return null;
            }

            var astJson = result.ParseTree.RootElement.GetRawText();
            if (!TryGetFirstStatement(result.ParseTree.RootElement, out var stmtObject))
            {
                Console.WriteLine($"Warning: Could not find first statement in {filePath}");
                return null;
            }

            var parsed = new ParsedSqlObject
            {
                Sql = sql,
                FilePath = filePath,
                AstJson = astJson
            };

            // Determine object type and extract schema/name from AST
            if (stmtObject.TryGetProperty("CreateSchemaStmt", out var createSchemaStmt))
            {
                parsed.ObjectType = SqlObjectType.Schema;
                var schemaName = GetStringProperty(createSchemaStmt, "schemaname") ?? defaultSchema;
                parsed.SchemaName = schemaName;
                parsed.ObjectName = schemaName;
            }
            else if (stmtObject.TryGetProperty("CreateStmt", out var createStmt))
            {
                parsed.ObjectType = SqlObjectType.Table;
                ExtractSchemaAndName(createStmt, "relation", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("IndexStmt", out var indexStmt))
            {
                parsed.ObjectType = SqlObjectType.Index;
                ExtractSchemaAndName(indexStmt, "relation", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = GetStringProperty(indexStmt, "idxname") ?? name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("ViewStmt", out var viewStmt))
            {
                parsed.ObjectType = SqlObjectType.View;
                ExtractSchemaAndName(viewStmt, "view", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CreateFunctionStmt", out var functionStmt))
            {
                parsed.ObjectType = SqlObjectType.Function;
                ExtractQualifiedName(functionStmt, "funcname", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CompositeTypeStmt", out var compositeTypeStmt))
            {
                parsed.ObjectType = SqlObjectType.Type;
                ExtractSchemaAndName(compositeTypeStmt, "typevar", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CreateEnumStmt", out var enumTypeStmt))
            {
                parsed.ObjectType = SqlObjectType.Type;
                ExtractQualifiedName(enumTypeStmt, "typeName", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CreateDomainStmt", out var domainTypeStmt))
            {
                parsed.ObjectType = SqlObjectType.Type;
                ExtractQualifiedName(domainTypeStmt, "domainname", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CreateSeqStmt", out var sequenceStmt))
            {
                parsed.ObjectType = SqlObjectType.Sequence;
                ExtractSchemaAndName(sequenceStmt, "sequence", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = name ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CreateTrigStmt", out var triggerStmt))
            {
                parsed.ObjectType = SqlObjectType.Trigger;
                ExtractSchemaAndName(triggerStmt, "relation", defaultSchema, out var schema, out var name);
                parsed.SchemaName = schema ?? defaultSchema;
                parsed.ObjectName = GetStringProperty(triggerStmt, "trigname") ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("CreateRoleStmt", out _))
            {
                parsed.ObjectType = SqlObjectType.Role;
                parsed.SchemaName = ""; // Roles are not schema-scoped
                parsed.ObjectName = ExtractRoleName(sql) ?? "unknown";
            }
            else if (stmtObject.TryGetProperty("GrantStmt", out _) && sql.Contains("GRANT", StringComparison.OrdinalIgnoreCase))
            {
                parsed.ObjectType = SqlObjectType.Permission;
                parsed.SchemaName = defaultSchema; // Will be refined later
                parsed.ObjectName = "_permissions";
            }
            else if (stmtObject.TryGetProperty("AlterOwnerStmt", out _) ||
                     (stmtObject.TryGetProperty("AlterTableStmt", out _) && sql.Contains("OWNER", StringComparison.OrdinalIgnoreCase)))
            {
                parsed.ObjectType = SqlObjectType.Owner;
                parsed.SchemaName = defaultSchema; // Will be refined later
                parsed.ObjectName = "_owners";
            }
            else if (stmtObject.TryGetProperty("CommentStmt", out _))
            {
                parsed.ObjectType = SqlObjectType.Comment;
                parsed.SchemaName = defaultSchema; // Will be refined later
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

    private static bool TryGetFirstStatement(JsonElement root, out JsonElement stmtObject)
    {
        stmtObject = default;

        if (!root.TryGetProperty("stmts", out var stmts) || stmts.GetArrayLength() == 0)
        {
            return false;
        }

        var firstStmt = stmts[0];
        if (!firstStmt.TryGetProperty("stmt", out stmtObject))
        {
            return false;
        }

        return true;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return null;
        }

        var value = propertyValue.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void ExtractSchemaAndName(JsonElement element, string propertyName, string defaultSchema, out string? schema, out string? name)
    {
        schema = null;
        name = null;

        if (!element.TryGetProperty(propertyName, out var relation))
        {
            return;
        }

        schema = GetStringProperty(relation, "schemaname") ?? defaultSchema;
        name = GetStringProperty(relation, "relname");
    }

    private static void ExtractQualifiedName(JsonElement element, string propertyName, string defaultSchema, out string? schema, out string? name)
    {
        schema = defaultSchema;
        name = null;

        if (!element.TryGetProperty(propertyName, out var nameArray))
        {
            return;
        }

        var nameParts = new List<string>();
        foreach (var item in nameArray.EnumerateArray())
        {
            if (item.TryGetProperty("String", out var stringNode))
            {
                var part = GetStringProperty(stringNode, "sval");
                if (!string.IsNullOrWhiteSpace(part))
                {
                    nameParts.Add(part);
                }
            }
        }

        if (nameParts.Count == 0)
        {
            return;
        }

        name = nameParts[^1];
        if (nameParts.Count > 1)
        {
            schema = nameParts[^2];
        }
    }

    private static string? ExtractRoleName(string sql)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"CREATE\s+ROLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?<role>""[^""]+""|[a-zA-Z_][a-zA-Z0-9_]*)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success
            ? match.Groups["role"].Value.Trim('"')
            : null;
    }

    private static (string? ObjectType, string? QualifiedName, string? Owner) ExtractExplicitOwner(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return (null, null, null);
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"ALTER\s+(?<type>SCHEMA|TABLE|VIEW|MATERIALIZED\s+VIEW|FUNCTION|PROCEDURE|SEQUENCE|TYPE)\s+(?<name>.+?)\s+OWNER\s+TO\s+(?<owner>""[^""]+""|[a-zA-Z_][a-zA-Z0-9_]*)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        if (!match.Success)
        {
            return (null, null, null);
        }

        return (
            match.Groups["type"].Value.Trim(),
            match.Groups["name"].Value.Trim().TrimEnd(';'),
            match.Groups["owner"].Value.Trim().Trim('"'));
    }

    private string GetRequiredPostgresVersion(XDocument doc)
    {
        var pgVersionElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "PostgresVersion");

        if (pgVersionElement == null || string.IsNullOrWhiteSpace(pgVersionElement.Value))
        {
            throw new InvalidOperationException(
                $"Project '{_projectPath}' must define a PostgresVersion property in the project file. Example: <PostgresVersion>16</PostgresVersion>.");
        }

        return pgVersionElement.Value.Trim();
    }

    private static string GetDefaultSchema(XDocument doc)
    {
        var defaultSchemaElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DefaultSchema");

        return string.IsNullOrWhiteSpace(defaultSchemaElement?.Value)
            ? "public"
            : defaultSchemaElement.Value.Trim();
    }

    private Parser CreateParser(string projectPostgresVersion)
    {
        var majorVersionText = projectPostgresVersion
            .Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!int.TryParse(majorVersionText, out var majorVersion))
        {
            throw new InvalidOperationException(
                $"Project '{_projectPath}' has an invalid PostgresVersion value '{projectPostgresVersion}'. Use a supported major version such as 16 or 17.");
        }

        var version = majorVersion switch
        {
            16 => PostgreSqlVersion.Postgres16,
            17 => PostgreSqlVersion.Postgres17,
            _ => throw new NotSupportedException(
                $"Project '{_projectPath}' targets PostgreSQL {majorVersion}, but only PostgreSQL 16 and 17 are currently supported.")
        };

        return new Parser(version);
    }

    /// <summary>
    /// Extracts schema and object name from RangeVar.
    /// </summary>
    private void ExtractSchemaAndName(RangeVar? rangeVar, out string? schema, out string? name)
    {
        schema = rangeVar?.Schemaname;
        name = rangeVar?.Relname;

        // Handle empty strings from protobuf deserialization
        if (string.IsNullOrWhiteSpace(schema))
        {
            schema = null;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = null;
        }
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
        if (obj.ObjectType == SqlObjectType.Owner)
        {
            ApplyOwnerStatement(schema, obj.Sql);
            return;
        }

        await ParseSqlFileAsync(obj.Sql, obj.FilePath, schema);
    }

    private static void ApplyOwnerStatement(PgSchema schema, string sql)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var (objectType, qualifiedName, owner) = ExtractExplicitOwner(sql);
        if (string.IsNullOrWhiteSpace(objectType) || string.IsNullOrWhiteSpace(qualifiedName) || string.IsNullOrWhiteSpace(owner))
        {
            return;
        }

        var normalizedType = objectType.ToUpperInvariant();
        var normalizedName = qualifiedName.Trim();

        switch (normalizedType)
        {
            case "SCHEMA":
                if (NameEquals(schema.Name, normalizedName))
                {
                    schema.Owner = owner;
                }
                break;

            case "TABLE":
                ApplyOwner(schema.Tables, normalizedName, owner);
                break;

            case "VIEW":
            case "MATERIALIZED VIEW":
                ApplyOwner(schema.Views, normalizedName, owner);
                break;

            case "FUNCTION":
            case "PROCEDURE":
                ApplyOwner(schema.Functions, normalizedName, owner);
                break;

            case "SEQUENCE":
                ApplyOwner(schema.Sequences, normalizedName, owner);
                break;

            case "TYPE":
                ApplyOwner(schema.Types, normalizedName, owner);
                break;
        }
    }

    private static void ApplyOwner<T>(IEnumerable<T> objects, string qualifiedName, string owner) where T : class
    {
        var name = ExtractObjectName(qualifiedName);
        var target = objects.FirstOrDefault(obj => NameEquals(GetName(obj), qualifiedName) || NameEquals(GetName(obj), name));
        if (target == null)
        {
            return;
        }

        SetOwner(target, owner);
    }

    private static string ExtractObjectName(string qualifiedName)
    {
        var parts = qualifiedName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? qualifiedName : parts[^1].Trim('"');
    }

    private static bool NameEquals(string? left, string? right)
    {
        return string.Equals(left?.Trim('"'), right?.Trim('"'), StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetName<T>(T obj) where T : class
    {
        return obj switch
        {
            PgTable table => table.Name,
            PgView view => view.Name,
            PgFunction function => function.Name,
            PgSequence sequence => sequence.Name,
            PgType type => type.Name,
            _ => null
        };
    }

    private static void SetOwner<T>(T obj, string owner) where T : class
    {
        switch (obj)
        {
            case PgTable table:
                table.Owner = owner;
                break;
            case PgView view:
                view.Owner = owner;
                break;
            case PgFunction function:
                function.Owner = owner;
                break;
            case PgSequence sequence:
                sequence.Owner = owner;
                break;
            case PgType type:
                type.Owner = owner;
                break;
        }
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
        var sqlFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get pre/post deployment scripts to exclude them from main objects
        var prePostScripts = GetPrePostDeploymentScripts(doc);
        var excludeFiles = new HashSet<string>(prePostScripts.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);

        var configuredSqlFiles = GetConfiguredSqlFiles(doc);
        foreach (var configuredFile in configuredSqlFiles)
        {
            if (ShouldIncludeProjectFile(configuredFile, excludeFiles))
            {
                sqlFiles.Add(configuredFile);
            }
        }

        // Scan convention-based SQL files recursively in project directory
        if (Directory.Exists(_projectDirectory))
        {
            foreach (var pattern in new[] { "*.sql", "*.pgsql" })
            {
                foreach (var file in Directory.GetFiles(_projectDirectory, pattern, SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(_projectDirectory, file);
                    if (ShouldIncludeProjectFile(relativePath, excludeFiles))
                    {
                        sqlFiles.Add(relativePath);
                    }
                }
            }
        }

        return sqlFiles.ToList();
    }

    private List<string> GetConfiguredSqlFiles(XDocument doc)
    {
        return doc.Descendants()
            .Where(e => e.Attribute("Include") != null)
            .Where(e => IsSupportedSqlElement(e.Name.LocalName))
            .Select(e => e.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => ExpandConfiguredSqlFiles(value!))
            .ToList();
    }

    private IEnumerable<string> ExpandConfiguredSqlFiles(string includeValue)
    {
        if (!includeValue.Contains('*') && !includeValue.Contains('?'))
        {
            yield return includeValue;
            yield break;
        }

        var normalizedPattern = includeValue.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(normalizedPattern)
            .Replace(@"\*\*", "__DOUBLE_WILDCARD__")
            .Replace(@"\*", $"[^{System.Text.RegularExpressions.Regex.Escape(Path.DirectorySeparatorChar.ToString())}]*")
            .Replace(@"\?", ".")
            .Replace("__DOUBLE_WILDCARD__", ".*") + "$";

        var matcher = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var file in Directory.EnumerateFiles(_projectDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_projectDirectory, file);
            var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (matcher.IsMatch(normalizedRelativePath))
            {
                yield return relativePath;
            }
        }
    }

    private static bool IsSupportedSqlElement(string elementName)
    {
        return elementName is "None" or "Content" or "SqlFile";
    }

    private static bool ShouldIncludeProjectFile(string relativePath, HashSet<string> excludeFiles)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var extension = Path.GetExtension(normalizedPath);
        if (!string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".pgsql", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains($"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith(".vs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !excludeFiles.Contains(relativePath) && !excludeFiles.Contains(normalizedPath);
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
            using var parser = CreateParser(GetRequiredPostgresVersion(XDocument.Load(_projectPath)));
            var defaultSchema = GetDefaultSchema(XDocument.Load(_projectPath));
            var result = parser.Parse(sql);

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

                await ProcessStatementAsync(stmt, sql, fileName, schema, defaultSchema);
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
    private async Task ProcessStatementAsync(JsonElement stmt, string fullSql, string fileName, PgSchema schema, string defaultSchema)
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
                Definition = tableDefinition ?? fullSql.Trim()
            };

            schema.Tables.Add(table);
            RegisterSourceLocation(schema.Name, table.Name, fileName);
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
                IsMaterialized = isMaterialized
            };

            schema.Views.Add(view);
            RegisterSourceLocation(schema.Name, view.Name, fileName);
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
                Definition = functionDefinition ?? fullSql.Trim()
            };

            schema.Functions.Add(function);
            RegisterSourceLocation(schema.Name, function.Name, fileName);
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
                Kind = PgTypeKind.Composite
            };

            schema.Types.Add(type);
            RegisterSourceLocation(schema.Name, type.Name, fileName);
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
                Kind = PgTypeKind.Enum
            };

            schema.Types.Add(type);
            RegisterSourceLocation(schema.Name, type.Name, fileName);
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
                Kind = PgTypeKind.Domain
            };

            schema.Types.Add(type);
            RegisterSourceLocation(schema.Name, type.Name, fileName);
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
                Definition = sequenceDefinition ?? fullSql.Trim()
            };

            schema.Sequences.Add(sequence);
            RegisterSourceLocation(schema.Name, sequence.Name, fileName);
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
                Definition = triggerDefinition ?? fullSql.Trim()
            };

            schema.Triggers.Add(trigger);
            RegisterSourceLocation(schema.Name, trigger.Name, fileName);
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

    private void RegisterSourceLocation(string schemaName, string objectName, string fileName)
    {
        if (_currentProject == null || string.IsNullOrWhiteSpace(schemaName) || string.IsNullOrWhiteSpace(objectName))
        {
            return;
        }

        _currentProject.RegisterSourceLocation($"{schemaName}.{objectName}", fileName);
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
