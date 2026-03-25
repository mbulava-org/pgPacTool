using mbulava.PostgreSql.Dac.Compile;
using System;
using System.Globalization;
using System.IO;
using OutputFormat = mbulava.PostgreSql.Dac.Compile.OutputFormat;

namespace MSBuild.Sdk.PostgreSql;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 5)
        {
            Console.Error.WriteLine("Expected arguments: <projectFile> <outputPath> <outputFormat> <databaseName> <validateOnBuild>");
            return 1;
        }

        var projectFile = args[0];
        var outputPath = args[1];
        var outputFormatString = args[2];
        var databaseName = args[3];
        var validateOnBuild = bool.TryParse(args[4], out var parsedValidateOnBuild) && parsedValidateOnBuild;

        try
        {
            Console.WriteLine($"Compiling PostgreSQL project: {databaseName}");
            Console.WriteLine($"  Project: {projectFile}");
            Console.WriteLine($"  Output: {outputPath}");
            Console.WriteLine($"  Format: {outputFormatString}");

            if (!File.Exists(projectFile))
            {
                Console.Error.WriteLine($"Project file not found: {projectFile}");
                return 1;
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var format = string.Equals(outputFormatString, "json", StringComparison.OrdinalIgnoreCase)
                ? OutputFormat.Json
                : OutputFormat.DacPac;

            var loader = new CsprojProjectLoader(projectFile);

            Console.WriteLine("  Loading project...");
            var project = loader.LoadProjectAsync().GetAwaiter().GetResult();

            Console.WriteLine($"  Loaded {project.Schemas.Count.ToString(CultureInfo.InvariantCulture)} schema(s)");

            foreach (var schema in project.Schemas)
            {
                var objectCount = schema.Tables.Count + schema.Views.Count + schema.Functions.Count + schema.Types.Count + schema.Sequences.Count + schema.Triggers.Count;
                Console.WriteLine($"  Schema '{schema.Name}': {objectCount.ToString(CultureInfo.InvariantCulture)} objects");
            }

            if (validateOnBuild)
            {
                Console.WriteLine("  Validating project...");
                var compiler = new ProjectCompiler();
                var result = compiler.Compile(project);

                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"WARNING {warning.Code}: {warning.Message}");
                }

                if (result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.Error.WriteLine($"ERROR {error.Code}: {error.Message}");
                    }

                    return 1;
                }

                Console.WriteLine($"  Validation passed: {result.DeploymentOrder.Count.ToString(CultureInfo.InvariantCulture)} objects in dependency order");
            }

            Console.WriteLine($"  Generating {format} output...");
            var actualOutput = loader.CompileAndGenerateOutputAsync(outputPath, format).GetAwaiter().GetResult();
            var fileInfo = new FileInfo(actualOutput);

            Console.WriteLine($"Successfully created: {Path.GetFileName(actualOutput)} ({fileInfo.Length.ToString("N0", CultureInfo.InvariantCulture)} bytes)");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
