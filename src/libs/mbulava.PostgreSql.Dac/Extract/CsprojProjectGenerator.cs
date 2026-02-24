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
        // Create folder structure and SQL files
        foreach (var schema in project.Schemas)
        {
            await GenerateSchemaFilesAsync(schema);
        }

        // Generate .csproj file
        GenerateCsprojFile(project);

        Console.WriteLine($"✅ Generated SDK-style project in: {_projectDirectory}");
        Console.WriteLine($"   📁 Schemas: {project.Schemas.Count}");
        Console.WriteLine($"   📄 SQL files created");
        Console.WriteLine($"   📦 Project file: {_projectName}.csproj");
    }

    /// <summary>
    /// Generates SQL files for a schema in the appropriate folder structure.
    /// </summary>
    private async Task GenerateSchemaFilesAsync(PgSchema schema)
    {
        var schemaDir = Path.Combine(_projectDirectory, schema.Name);
        
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
                    new XElement("DatabaseName", project.DatabaseName ?? _projectName)
                ),

                // Comment about auto-discovery
                new XComment(@" 
    Convention: All .sql files are automatically included!
    No need to explicitly list them - just organize in folders.
    
    Folder structure:
    - {schema}/Tables/
    - {schema}/Views/
    - {schema}/Functions/
    - {schema}/Types/
    - {schema}/Sequences/
    - {schema}/Triggers/
  "),

                // ItemGroup for Pre/Post deployment scripts (placeholder)
                new XComment(" Only Pre/Post deployment scripts need explicit configuration "),
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
        }

        stats.TotalFiles = stats.Tables + stats.Views + stats.Functions + 
                          stats.Types + stats.Sequences + stats.Triggers;

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
    public int TotalFiles { get; set; }
}
