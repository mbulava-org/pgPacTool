using mbulava.PostgreSql.Dac.Models;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    public static class PgProjectWriter
    {
        public static async Task WriteAsProjectAsync(PgProject project, string outputPath)
        {
            foreach (var table in project.Tables)
            {
                string dir = Path.Combine(outputPath, table.Schema, "Tables");
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, $"{table.Name}.sql"), table.Definition, Encoding.UTF8);
            }

            foreach (var view in project.Views)
            {
                string dir = Path.Combine(outputPath, view.Schema, "Views");
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, $"{view.Name}.sql"), view.Definition, Encoding.UTF8);
            }

            foreach (var func in project.Functions)
            {
                string dir = Path.Combine(outputPath, func.Schema, "Functions");
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, $"{func.Name}.sql"), func.Definition, Encoding.UTF8);
            }

            foreach (var trig in project.Triggers)
            {
                string dir = Path.Combine(outputPath, trig.Table, "Triggers");
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, $"{trig.Name}.sql"), trig.Definition, Encoding.UTF8);
            }
        }

        public static void WriteAsPackage(string projectPath, string packagePath)
        {
            if (File.Exists(packagePath))
                File.Delete(packagePath);

            ZipFile.CreateFromDirectory(projectPath, packagePath, CompressionLevel.Optimal, includeBaseDirectory: true);
        }
    }

}
