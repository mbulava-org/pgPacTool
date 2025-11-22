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
            throw new NotImplementedException();
        }

        public static void WriteAsPackage(string projectPath, string packagePath)
        {
            if (File.Exists(packagePath))
                File.Delete(packagePath);

            ZipFile.CreateFromDirectory(projectPath, packagePath, CompressionLevel.Optimal, includeBaseDirectory: true);
        }
    }

}
