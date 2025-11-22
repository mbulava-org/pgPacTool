using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Models
{
    public abstract record PgDbObject(string Name, string Definition);

    public record ObjectDiff(string Name, DiffType Type, string Script);

    public record TableDiff(string Name, DiffType Type, string Script) : ObjectDiff(Name, Type, Script);

    public enum DiffType { Missing, Changed, Extra }

    public class SchemaDiff
    {
        public List<PgSchema> MissingSchemas { get; } = new();
        
        public List<ObjectDiff> TypeDiffs { get; } = new();
        public List<ObjectDiff> SequenceDiffs { get; } = new();
        public List<ObjectDiff> ColumnDiffs { get; } = new();
        public List<ObjectDiff> TableDiffs { get; } = new();

        public List<ObjectDiff> ViewDiffs { get; } = new();
        public List<ObjectDiff> FunctionDiffs { get; } = new();
        public List<ObjectDiff> TriggerDiffs { get; } = new();

        public List<ObjectDiff> IndexDiffs { get; } = new();
        public List<ObjectDiff> ConstraintDiffs { get; } = new();
    }


}
