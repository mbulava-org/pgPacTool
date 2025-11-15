using mbulava.PostgreSql.Dac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    public static class PgManifestWriter
    {
        public static async Task WriteManifestAsync(PgProject project, string outputPath, string databaseName, string postgresVersion)
        {
            var manifest = new
            {
                database = databaseName,
                postgresVersion,
                extractionDate = DateTime.UtcNow,
                schemas = project.Tables
                    .Select(t => t.Schema)
                    .Distinct()
                    .Select(schema => new {
                        name = schema,
                        tables = project.Tables.Where(x => x.Schema == schema).Select(x => x.Name),
                        views = project.Views.Where(x => x.Schema == schema).Select(x => x.Name),
                        functions = project.Functions.Where(x => x.Schema == schema).Select(x => x.Name),
                        triggers = project.Triggers.Where(x => x.Schema == schema).Select(x => x.Name)
                    })
            };

            string json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            string manifestPath = Path.Combine(outputPath, "manifest.json");
            await File.WriteAllTextAsync(manifestPath, json);
        }
    }
}
