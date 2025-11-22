using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Models
{
    public record PgTable(string Name,  string Definition) : PgDbObject(Name, Definition)
    {
        public List<PgColumn> Columns { get; } = new();
        public List<PgIndex> Indexes { get; } = new();
        public List<PgConstraint> Constraints { get; } = new();

    };
    public record PgColumn(string Name, string DataType, bool IsNullable, string? DefaultValue);
    public record PgIndex(string Name, string TableName, string Definition) : PgDbObject(Name, Definition);

    public record PgConstraint(string Name, string TableName, string Type, string Definition) : PgDbObject(Name, Definition)
    {
        public string? ReferencedTable { get; init; } 

    };

    public record PgView(string Name, string Definition) : PgDbObject(Name, Definition);
    public record PgFunction(string Name, string Definition): PgDbObject(Name, Definition);
    public record PgTrigger(string Name, string Table, string Definition) : PgDbObject($"{Table}.{Name}", Definition);

    public record PgType(string Name, string Schema, string Kind, string Definition);
    public record PgSequence(string Name, string Schema, string Definition);

    public class PgSchema
    {
        public string Name { get; set; } = "public";

        public List<PgTable> Tables { get; } = new();
        public List<PgView> Views { get; } = new();
        public List<PgFunction> Functions { get; } = new();
        public List<PgTrigger> Triggers { get; } = new();

        public List<PgType> Types { get; } = new();   // NEW
        public List<PgSequence> Sequences { get; } = new();   // NEW
        

    }


    public class PgProject
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string PostgresVersion { get; set; } = string.Empty;

        public DateTimeOffset ExtractionDate { get; set; } = DateTimeOffset.MinValue;

        public List<PgSchema> Schemas { get; } = new();
        
    }
}
