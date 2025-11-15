using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    public class ProjectExtract
    {
        private readonly NpgsqlConnection _conn;

        public ProjectExtract(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task ExtractAsync(string outputPath)
        {
            var extractor = new PgSchemaExtractor(_conn);
            var project = await extractor.ExtractAllSchemasAsync(_conn.Database, "17.0");

            await PgProjectWriter.WriteAsProjectAsync(project, outputPath);
            await PgManifestWriter.WriteManifestAsync(project, outputPath, _conn.Database, "17.0");
        }
    }
}
