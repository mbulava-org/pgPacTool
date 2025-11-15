using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Models
{
    public record PgTable(string Name,  string Definition);
    public record PgView(string Name, string Definition);
    public record PgFunction(string Name, string Definition);
    public record PgTrigger(string Name, string Table, string Definition);

    public class PgSchema
    {
        public string Name { get; set; } = string.Empty;

        public List<PgTable> Tables { get; } = new();
        public List<PgView> Views { get; } = new();
        public List<PgFunction> Functions { get; } = new();
        public List<PgTrigger> Triggers { get; } = new();

    }


    public class PgProject
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string PostgresVersion { get; set; } = string.Empty;

        public List<PgSchema> Schemas { get; } = new();
        
    }
}
