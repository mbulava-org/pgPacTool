using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    public class PackageExtract
    {
        private readonly NpgsqlConnection _conn;

        public PackageExtract(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task ExtractAsync(string packagePath)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            var extractor = new PgSchemaExtractor(_conn);
            var project = await extractor.ExtractAllSchemasAsync(_conn.Database, "17.0");

            await PgProjectWriter.WriteAsProjectAsync(project, tempPath);
            await PgManifestWriter.WriteManifestAsync(project, tempPath, _conn.Database, "17.0");

            PgProjectWriter.WriteAsPackage(tempPath, packagePath);
            Directory.Delete(tempPath, recursive: true);
        }
    }
}
