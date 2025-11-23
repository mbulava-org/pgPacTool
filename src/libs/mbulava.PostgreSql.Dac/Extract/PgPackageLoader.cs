using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using Npgquery.Native;
using PgQuery;
using System.IO.Compression;
using System.Text.Json;


namespace mbulava.PostgreSql.Dac.Extract
{
    

    public class PgPackageLoader
    {
        public async Task<PgProject> LoadAsync(Stream input)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return await JsonSerializer.DeserializeAsync<PgProject>(input, options)
                   ?? throw new InvalidOperationException("Invalid pgPac file");
        }

    }
}
