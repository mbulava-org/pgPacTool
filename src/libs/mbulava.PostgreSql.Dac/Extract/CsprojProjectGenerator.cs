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

        // Tables
        if (schema.Tables.Count > 0)
        {
            var tablesDir = Path.Combine(schemaDir, "Tables");
            Directory.CreateDirectory(tablesDir);

            foreach (var table in schema.Tables)
            {
                var filePath = Path.Combine(tablesDir, $"{table.Name}.sql");
                await File.WriteAllTextAsync(filePath, table.Definition, Encoding.UTF8);
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
        var csproj = new XDocument(
            new XElement("Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk"),

                // PropertyGroup
                new XElement("PropertyGroup",
                    new XElement("TargetFramework", "net10.0"),
                    new XElement("OutputType", "Library"),
                    new XElement("IsPackable", "false"),
                    new XElement("DatabaseName", project.DatabaseName ?? _projectName),
                    new XElement("PostgresVersion", project.PostgresVersion ?? "16.0"),
                    new XComment(" PostgreSQL target version - used for compilation and deployment validation ")
                ),

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
            if (schema.Privileges.Count > 0 || schema.Tables.Any(t => t.Privileges.Count > 0))
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
