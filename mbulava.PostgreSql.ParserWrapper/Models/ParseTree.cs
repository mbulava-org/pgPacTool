using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.ParserWrapper.Models
{
    using System.Text.Json.Serialization;

    public class ParseTree
    {
        [JsonPropertyName("stmts")]
        public List<RawStmt> Statements { get; set; }
    }

    public class RawStmt
    {
        [JsonPropertyName("stmt")]
        public Statement Statement { get; set; }
    }

    public class Statement
    {
        [JsonPropertyName("SelectStmt")]
        public SelectStmt Select { get; set; }
    }

    public class SelectStmt
    {
        [JsonPropertyName("targetList")]
        public List<TargetEntry> TargetList { get; set; }

        [JsonPropertyName("fromClause")]
        public List<FromItem> FromClause { get; set; }
    }

    public class TargetEntry
    {
        [JsonPropertyName("ResTarget")]
        public ResTarget ResTarget { get; set; }
    }

    public class ResTarget
    {
        [JsonPropertyName("val")]
        public ColumnRef Value { get; set; }
    }

    public class ColumnRef
    {
        [JsonPropertyName("ColumnRef")]
        public ColumnRefDetail ColumnRefDetail { get; set; }
    }

    public class ColumnRefDetail
    {
        [JsonPropertyName("fields")]
        public List<object> Fields { get; set; } // Can be string or A_Star
    }

    public class FromItem
    {
        [JsonPropertyName("RangeVar")]
        public RangeVar RangeVar { get; set; }
    }

    public class RangeVar
    {
        [JsonPropertyName("relname")]
        public string RelationName { get; set; }

        [JsonPropertyName("schemaname")]
        public string SchemaName { get; set; }
    }
}
