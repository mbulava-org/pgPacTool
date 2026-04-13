using mbulava.PostgreSql.Dac.Models;
using System.Text;
using System.Xml.Linq;

namespace mbulava.PostgreSql.Dac.Extract;

/// <summary>
/// Generates SDK-style .csproj projects from extracted PgProject.
/// Creates folder structure: /{schema}/{ObjectType}/{ObjectName}.sql
/// </summary>
public class CsprojProjectGenerator
{
    private const string MsBuildSdkPackageVersion = "1.0.0-preview5";
    private readonly string _projectDirectory;
    private readonly string _projectName;

    public CsprojProjectGenerator(string projectPath)
    {
        _projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new ArgumentException("Invalid project path");
        _projectName = Path.GetFileNameWithoutExtension(projectPath);
        
        // Create project directory if it doesn't exist
        Directory.CreateDirectory(_projectDirectory);
    }

    /// <summary>
    /// Generates a .csproj project with folder structure from PgProject.
    /// </summary>
    public async Task GenerateProjectAsync(PgProject project)
    {
        // Create folder structure and SQL files for schemas
        foreach (var schema in project.Schemas)
        {
            await GenerateSchemaFilesAsync(schema);
        }

        // Generate roles and permissions
        await GenerateRolesAndPermissionsAsync(project);

        // Generate .csproj file
        GenerateCsprojFile(project);

        Console.WriteLine($"✅ Generated SDK-style project in: {_projectDirectory}");
        Console.WriteLine($"   📁 Schemas: {project.Schemas.Count}");
        Console.WriteLine($"   👤 Roles: {project.Roles.Count}");
        Console.WriteLine($"   📄 SQL files created");
        Console.WriteLine($"   📦 Project file: {_projectName}.csproj");
    }

    /// <summary>
    /// Generates SQL files for a schema in the appropriate folder structure.
    /// </summary>
    private async Task GenerateSchemaFilesAsync(PgSchema schema)
    {
        var schemaDir = Path.Combine(_projectDirectory, schema.Name);
        Directory.CreateDirectory(schemaDir);

        // Generate CREATE SCHEMA statement file
        // Use _schema.sql so it sorts first (underscore before letters)
        var schemaFilePath = Path.Combine(schemaDir, "_schema.sql");

        // Use the original SQL definition from extraction (already properly quoted from AST)
        var schemaDefinition = !string.IsNullOrWhiteSpace(schema.Definition) 
            ? schema.Definition 
            : $"CREATE SCHEMA IF NOT EXISTS {schema.Name} AUTHORIZATION {schema.Owner};";

        await File.WriteAllTextAsync(schemaFilePath, schemaDefinition, Encoding.UTF8);

        // Generate owner ALTER statements if any objects have different owners
        await GenerateOwnerStatementsAsync(schema);

        // Tables
        if (schema.Tables.Count > 0)
        {
            var tablesDir = Path.Combine(schemaDir, "Tables");
            Directory.CreateDirectory(tablesDir);

            foreach (var table in schema.Tables)
            {
                var filePath = Path.Combine(tablesDir, $"{table.Name}.sql");
                var tableDefinition = new StringBuilder();

                // Add CREATE TABLE statement
                tableDefinition.AppendLine(table.Definition);

                // Add column comments if any exist
                var columnsWithComments = table.Columns.Where(c => !string.IsNullOrWhiteSpace(c.Comment)).ToList();
                if (columnsWithComments.Count > 0)
                {
                    tableDefinition.AppendLine();
                    tableDefinition.AppendLine($"-- Column comments for {schema.Name}.{table.Name}");
                    foreach (var column in columnsWithComments)
                    {
                        var escapedComment = column.Comment!.Replace("'", "''");
                        tableDefinition.AppendLine($"COMMENT ON COLUMN {schema.Name}.{table.Name}.{column.Name} IS '{escapedComment}';");
                    }
                }

                await File.WriteAllTextAsync(filePath, tableDefinition.ToString(), Encoding.UTF8);
            }
        }

        // Indexes (from all tables in the schema)
        var allIndexes = schema.Tables.SelectMany(t => t.Indexes).ToList();
        if (allIndexes.Count > 0)
        {
            var indexesDir = Path.Combine(schemaDir, "Indexes");
            Directory.CreateDirectory(indexesDir);

            foreach (var index in allIndexes)
            {
                var filePath = Path.Combine(indexesDir, $"{index.Name}.sql");
                await File.WriteAllTextAsync(filePath, index.Definition, Encoding.UTF8);
            }
        }

        // Views
        if (schema.Views.Count > 0)
        {
            var viewsDir = Path.Combine(schemaDir, "Views");
            Directory.CreateDirectory(viewsDir);
            
            foreach (var view in schema.Views)
            {
                var filePath = Path.Combine(viewsDir, $"{view.Name}.sql");
                await File.WriteAllTextAsync(filePath, view.Definition, Encoding.UTF8);
            }
        }

        // Functions
        if (schema.Functions.Count > 0)
        {
            var functionsDir = Path.Combine(schemaDir, "Functions");
            Directory.CreateDirectory(functionsDir);
            
            foreach (var function in schema.Functions)
            {
                var filePath = Path.Combine(functionsDir, $"{function.Name}.sql");
                await File.WriteAllTextAsync(filePath, function.Definition, Encoding.UTF8);
            }
        }

        // Types
        if (schema.Types.Count > 0)
        {
            var typesDir = Path.Combine(schemaDir, "Types");
            Directory.CreateDirectory(typesDir);
            
            foreach (var type in schema.Types)
            {
                var filePath = Path.Combine(typesDir, $"{type.Name}.sql");
                await File.WriteAllTextAsync(filePath, type.Definition, Encoding.UTF8);
            }
        }

        // Sequences
        if (schema.Sequences.Count > 0)
        {
            var sequencesDir = Path.Combine(schemaDir, "Sequences");
            Directory.CreateDirectory(sequencesDir);
            
            foreach (var sequence in schema.Sequences)
            {
                var filePath = Path.Combine(sequencesDir, $"{sequence.Name}.sql");
                await File.WriteAllTextAsync(filePath, sequence.Definition, Encoding.UTF8);
            }
        }

        // Triggers
        if (schema.Triggers.Count > 0)
        {
            var triggersDir = Path.Combine(schemaDir, "Triggers");
            Directory.CreateDirectory(triggersDir);

            foreach (var trigger in schema.Triggers)
            {
                var filePath = Path.Combine(triggersDir, $"{trigger.Name}.sql");
                await File.WriteAllTextAsync(filePath, trigger.Definition, Encoding.UTF8);
            }
        }
    }

    /// <summary>
    /// Generates ALTER ... OWNER TO statements for objects with different owners than schema owner.
    /// </summary>
    private async Task GenerateOwnerStatementsAsync(PgSchema schema)
    {
        var ownerStatements = new StringBuilder();
        var hasOwners = false;

        // Check tables
        foreach (var table in schema.Tables.Where(t => t.Owner != schema.Owner))
        {
            ownerStatements.AppendLine($"ALTER TABLE {schema.Name}.{table.Name} OWNER TO {table.Owner};");
            hasOwners = true;
        }

        // Check views
        foreach (var view in schema.Views.Where(v => v.Owner != schema.Owner))
        {
            ownerStatements.AppendLine($"ALTER VIEW {schema.Name}.{view.Name} OWNER TO {view.Owner};");
            hasOwners = true;
        }

        // Check functions
        foreach (var function in schema.Functions.Where(f => f.Owner != schema.Owner))
        {
            ownerStatements.AppendLine($"ALTER FUNCTION {schema.Name}.{function.Name} OWNER TO {function.Owner};");
            hasOwners = true;
        }

        // Check sequences
        foreach (var sequence in schema.Sequences.Where(s => s.Owner != schema.Owner))
        {
            ownerStatements.AppendLine($"ALTER SEQUENCE {schema.Name}.{sequence.Name} OWNER TO {sequence.Owner};");
            hasOwners = true;
        }

        // Check types
        foreach (var type in schema.Types.Where(t => t.Owner != schema.Owner))
        {
            ownerStatements.AppendLine($"ALTER TYPE {schema.Name}.{type.Name} OWNER TO {type.Owner};");
            hasOwners = true;
        }

        // Check indexes (if owner differs from table owner)
        foreach (var table in schema.Tables)
        {
            foreach (var index in table.Indexes.Where(i => i.Owner != table.Owner))
            {
                ownerStatements.AppendLine($"ALTER INDEX {schema.Name}.{index.Name} OWNER TO {index.Owner};");
                hasOwners = true;
            }
        }

        // Only create file if there are owner statements
        if (hasOwners)
        {
            var schemaDir = Path.Combine(_projectDirectory, schema.Name);
            var ownerFilePath = Path.Combine(schemaDir, "_owners.sql");
            await File.WriteAllTextAsync(ownerFilePath, ownerStatements.ToString(), Encoding.UTF8);
        }
    }

    /// <summary>
    /// Generates SQL files for roles and permissions.
    /// </summary>
    private async Task GenerateRolesAndPermissionsAsync(PgProject project)
    {
        if (project.Roles.Count == 0)
            return;

        // Create Security directory for roles and permissions
        var securityDir = Path.Combine(_projectDirectory, "Security");
        Directory.CreateDirectory(securityDir);

        // Roles subdirectory
        var rolesDir = Path.Combine(securityDir, "Roles");
        Directory.CreateDirectory(rolesDir);

        foreach (var role in project.Roles)
        {
            var roleFilePath = Path.Combine(rolesDir, $"{role.Name}.sql");
            var roleDefinition = new StringBuilder();

            // Add CREATE ROLE statement
            roleDefinition.AppendLine(role.Definition);

            // Add role memberships (GRANT role TO role)
            if (role.MemberOf.Count > 0)
            {
                roleDefinition.AppendLine();
                roleDefinition.AppendLine($"-- Role memberships for {role.Name}");
                foreach (var parentRole in role.MemberOf)
                {
                    roleDefinition.AppendLine($"GRANT {parentRole} TO {role.Name};");
                }
            }

            await File.WriteAllTextAsync(roleFilePath, roleDefinition.ToString(), Encoding.UTF8);
        }

        // Generate permissions (GRANT statements) for each schema
        var permissionsDir = Path.Combine(securityDir, "Permissions");
        Directory.CreateDirectory(permissionsDir);

        foreach (var schema in project.Schemas)
        {
            var schemaPermissions = new StringBuilder();
            var hasPermissions = false;

            // Schema privileges
            if (schema.Privileges.Count > 0)
            {
                schemaPermissions.AppendLine($"-- Schema: {schema.Name}");
                foreach (var privilege in schema.Privileges)
                {
                    var grantOption = privilege.IsGrantable ? " WITH GRANT OPTION" : "";
                    schemaPermissions.AppendLine($"GRANT {privilege.PrivilegeType} ON SCHEMA {schema.Name} TO {privilege.Grantee}{grantOption};");
                }
                schemaPermissions.AppendLine();
                hasPermissions = true;
            }

            // Table privileges
            foreach (var table in schema.Tables)
            {
                if (table.Privileges.Count > 0)
                {
                    schemaPermissions.AppendLine($"-- Table: {schema.Name}.{table.Name}");
                    foreach (var privilege in table.Privileges)
                    {
                        var grantOption = privilege.IsGrantable ? " WITH GRANT OPTION" : "";
                        schemaPermissions.AppendLine($"GRANT {privilege.PrivilegeType} ON TABLE {schema.Name}.{table.Name} TO {privilege.Grantee}{grantOption};");
                    }
                    schemaPermissions.AppendLine();
                    hasPermissions = true;
                }
            }

            // View privileges
            foreach (var view in schema.Views)
            {
                if (view.Privileges.Count > 0)
                {
                    schemaPermissions.AppendLine($"-- View: {schema.Name}.{view.Name}");
                    foreach (var privilege in view.Privileges)
                    {
                        var grantOption = privilege.IsGrantable ? " WITH GRANT OPTION" : "";
                        schemaPermissions.AppendLine($"GRANT {privilege.PrivilegeType} ON TABLE {schema.Name}.{view.Name} TO {privilege.Grantee}{grantOption};");
                    }
                    schemaPermissions.AppendLine();
                    hasPermissions = true;
                }
            }

            // Function privileges
            foreach (var function in schema.Functions)
            {
                if (function.Privileges.Count > 0)
                {
                    schemaPermissions.AppendLine($"-- Function: {schema.Name}.{function.Name}");
                    foreach (var privilege in function.Privileges)
                    {
                        var grantOption = privilege.IsGrantable ? " WITH GRANT OPTION" : "";
                        schemaPermissions.AppendLine($"GRANT {privilege.PrivilegeType} ON FUNCTION {schema.Name}.{function.Name} TO {privilege.Grantee}{grantOption};");
                    }
                    schemaPermissions.AppendLine();
                    hasPermissions = true;
                }
            }

            // Sequence privileges
            foreach (var sequence in schema.Sequences)
            {
                if (sequence.Privileges.Count > 0)
                {
                    schemaPermissions.AppendLine($"-- Sequence: {schema.Name}.{sequence.Name}");
                    foreach (var privilege in sequence.Privileges)
                    {
                        var grantOption = privilege.IsGrantable ? " WITH GRANT OPTION" : "";
                        schemaPermissions.AppendLine($"GRANT {privilege.PrivilegeType} ON SEQUENCE {schema.Name}.{sequence.Name} TO {privilege.Grantee}{grantOption};");
                    }
                    schemaPermissions.AppendLine();
                    hasPermissions = true;
                }
            }

            // Type privileges
            foreach (var type in schema.Types)
            {
                if (type.Privileges.Count > 0)
                {
                    schemaPermissions.AppendLine($"-- Type: {schema.Name}.{type.Name}");
                    foreach (var privilege in type.Privileges)
                    {
                        var grantOption = privilege.IsGrantable ? " WITH GRANT OPTION" : "";
                        schemaPermissions.AppendLine($"GRANT {privilege.PrivilegeType} ON TYPE {schema.Name}.{type.Name} TO {privilege.Grantee}{grantOption};");
                    }
                    schemaPermissions.AppendLine();
                    hasPermissions = true;
                }
            }

            // Only create file if there are permissions
            if (hasPermissions)
            {
                var permissionFilePath = Path.Combine(permissionsDir, $"{schema.Name}.sql");
                await File.WriteAllTextAsync(permissionFilePath, schemaPermissions.ToString(), Encoding.UTF8);
            }
        }
    }

    /// <summary>
    /// Generates the .csproj file for the project.
    /// </summary>
    private void GenerateCsprojFile(PgProject project)
    {
        var projectPostgresVersion = GetProjectPostgresVersion(project.PostgresVersion);

        var csproj = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("Sdk",
                        new XAttribute("Name", "MSBuild.Sdk.PostgreSql"),
                        new XAttribute("Version", MsBuildSdkPackageVersion)),

                // PropertyGroup
                new XElement("PropertyGroup",
                    new XElement("TargetFramework", "net10.0"),
                    new XElement("DatabaseName", project.DatabaseName ?? _projectName),
                    new XElement("PostgresVersion", projectPostgresVersion),
                    new XComment(" PostgreSQL target version (major version only) - used for compilation and deployment validation "),
                    new XElement("DefaultSchema", GetProjectDefaultSchema(project.DefaultSchema)),
                    new XComment(" Default schema for objects that omit schema qualification "),
                    new XElement("DefaultOwner", project.DefaultOwner ?? "postgres"),
                    new XComment(" Default owner for objects that don't explicitly specify one "),
                    new XElement("DefaultTablespace", project.DefaultTablespace ?? "pg_default"),
                    new XComment(" Default tablespace for tables/indexes that don't explicitly specify one ")
                ),

                // Comment documenting source (not used in compilation, just for reference)
                new XComment($" Extracted from: {project.SourceConnection ?? "unknown"} "),

                // Comment about auto-discovery
                new XComment(@" 
    Convention: All .sql files are automatically included!
    No need to explicitly list them - just organize in folders.

    Folder structure:
    - {schema}/_schema.sql         (CREATE SCHEMA statement)
    - {schema}/Tables/             (CREATE TABLE statements)
    - {schema}/Indexes/            (CREATE INDEX statements)
    - {schema}/Views/              (CREATE VIEW statements)
    - {schema}/Functions/          (CREATE FUNCTION/PROCEDURE statements)
    - {schema}/Types/              (CREATE TYPE statements)
    - {schema}/Sequences/          (CREATE SEQUENCE statements)
    - {schema}/Triggers/           (CREATE TRIGGER statements)
    - Security/Roles/              (CREATE ROLE statements)
    - Security/Permissions/        (GRANT statements per schema)

    Default values (used if SQL doesn't specify):
    - DefaultOwner: Applied to objects without explicit OWNER clause
    - DefaultTablespace: Applied to tables/indexes without TABLESPACE clause

    Files are deployed in dependency order automatically.
  "),

                // ItemGroup for Pre/Post deployment scripts (placeholder)
                new XComment(" Pre/Post deployment scripts (optional) "),
                new XElement("ItemGroup",
                    new XComment(" <PreDeploy Include=\"Scripts\\PreDeployment\\*.sql\" /> "),
                    new XComment(" <PostDeploy Include=\"Scripts\\PostDeployment\\*.sql\" /> ")
                )
            )
        );

        var csprojPath = Path.Combine(_projectDirectory, $"{_projectName}.csproj");
        csproj.Save(csprojPath);
    }

    private static string GetProjectPostgresVersion(string? projectPostgresVersion)
    {
        if (string.IsNullOrWhiteSpace(projectPostgresVersion))
        {
            return "16";
        }

        var majorVersion = projectPostgresVersion
            .Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(majorVersion) ? "16" : majorVersion;
    }

    private static string GetProjectDefaultSchema(string? projectDefaultSchema)
    {
        return string.IsNullOrWhiteSpace(projectDefaultSchema)
            ? "public"
            : projectDefaultSchema.Trim();
    }

    /// <summary>
    /// Gets statistics about what was generated.
    /// </summary>
    public static GenerationStats GetStats(PgProject project)
    {
        var stats = new GenerationStats();

        foreach (var schema in project.Schemas)
        {
            stats.Schemas++;
            stats.Tables += schema.Tables.Count;
            stats.Views += schema.Views.Count;
            stats.Functions += schema.Functions.Count;
            stats.Types += schema.Types.Count;
            stats.Sequences += schema.Sequences.Count;
            stats.Triggers += schema.Triggers.Count;

            // Count indexes from all tables
            stats.Indexes += schema.Tables.Sum(t => t.Indexes.Count);

            // Count permissions per schema
            if (schema.Privileges.Count > 0 || 
                schema.Tables.Any(t => t.Privileges.Count > 0) ||
                schema.Views.Any(v => v.Privileges.Count > 0) ||
                schema.Functions.Any(f => f.Privileges.Count > 0) ||
                schema.Sequences.Any(s => s.Privileges.Count > 0) ||
                schema.Types.Any(t => t.Privileges.Count > 0))
            {
                stats.PermissionFiles++;
            }
        }

        stats.Roles = project.Roles.Count;

        // Total files includes:
        // - Schema definition files (_schema.sql for each schema)
        // - All object files (tables, views, functions, types, sequences, triggers, indexes)
        // - Role files (one per role in Security/Roles/)
        // - Permission files (one per schema with permissions in Security/Permissions/)
        stats.TotalFiles = stats.Schemas + stats.Tables + stats.Views + stats.Functions + 
                          stats.Types + stats.Sequences + stats.Triggers + stats.Indexes + 
                          stats.Roles + stats.PermissionFiles;

        return stats;
    }
}

/// <summary>
/// Statistics about generated project.
/// </summary>
public record GenerationStats
{
    public int Schemas { get; set; }
    public int Tables { get; set; }
    public int Views { get; set; }
    public int Functions { get; set; }
    public int Types { get; set; }
    public int Sequences { get; set; }
    public int Triggers { get; set; }
    public int Indexes { get; set; }
    public int Roles { get; set; }
    public int PermissionFiles { get; set; }
    public int TotalFiles { get; set; }
}
