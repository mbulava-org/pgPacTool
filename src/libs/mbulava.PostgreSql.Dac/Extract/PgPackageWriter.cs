using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    using mbulava.PostgreSql.Dac.Models;
    using System.IO.Compression;
    using System.Text.Json;

    public static class PgPackageWriter
    {
        public static void WriteAsPackage(PgProject project, string outputPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // Write manifest
            var manifestPath = Path.Combine(tempDir, "manifest.json");
            var manifestJson = JsonSerializer.Serialize(project,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, manifestJson);

            // Write all objects into /objects
            var objectsDir = Path.Combine(tempDir, "objects");
            Directory.CreateDirectory(objectsDir);

            foreach (var schema in project.Schemas)
            {
                foreach (var table in schema.Tables)
                    File.WriteAllText(Path.Combine(objectsDir, $"{schema.Name}_{table.Name}.sql"), table.Definition);

                foreach (var view in schema.Views)
                    File.WriteAllText(Path.Combine(objectsDir, $"{schema.Name}_{view.Name}.sql"), view.Definition);

                foreach (var fn in schema.Functions)
                    File.WriteAllText(Path.Combine(objectsDir, $"{schema.Name}_{fn.Name}.sql"), fn.Definition);

                foreach (var ty in schema.Types)
                    File.WriteAllText(Path.Combine(objectsDir, $"{schema.Name}_{ty.Name}.sql"), ty.Definition);

                foreach (var seq in schema.Sequences)
                    File.WriteAllText(Path.Combine(objectsDir, $"{schema.Name}_{seq.Name}.sql"), seq.Definition);

                foreach (var trig in schema.Triggers)
                    File.WriteAllText(Path.Combine(objectsDir, $"{schema.Name}_{trig.Name}.sql"), trig.Definition);
            }

            // Zip into pgpac
            if (File.Exists(outputPath)) File.Delete(outputPath);
            ZipFile.CreateFromDirectory(tempDir, outputPath);

            Directory.Delete(tempDir, true);
        }
    }
}
