using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compare
{
    public static class TargetLoader
    {
        public static async Task<PgProject> FromDatabaseAsync(NpgsqlConnection conn)
        {
            var extractor = new PgSchemaExtractor(conn);
            return await extractor.ExtractAllSchemasAsync(conn.Database, "17.0");
        }

        public static PgProject FromProjectFolder(string folderPath)
        {
            // Walk the folder structure and rebuild PgProject
            var project = new PgProject
            {
                DatabaseName = Path.GetFileNameWithoutExtension(folderPath),
                PostgresVersion = "unknown"
            };

            foreach (var schemaDir in Directory.GetDirectories(folderPath))
            {
                var schema = new PgSchema { Name = Path.GetFileName(schemaDir) };

                foreach (var file in Directory.GetFiles(Path.Combine(schemaDir, "Tables"), "*.sql"))
                    schema.Tables.Add(new PgTable(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file)));

                foreach (var file in Directory.GetFiles(Path.Combine(schemaDir, "Views"), "*.sql"))
                    schema.Views.Add(new PgView(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file)));

                // Functions, Triggers similarly...

                project.Schemas.Add(schema);
            }

            return project;
        }

        public static PgProject FromPackage(string packagePath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(packagePath, tempDir);
            var project = FromProjectFolder(tempDir);
            Directory.Delete(tempDir, recursive: true);
            return project;
        }
    }
}
