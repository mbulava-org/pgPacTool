using PgQuery;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace mbulava.PostgreSql.Dac.Models
{
    public class PgProject
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string PostgresVersion { get; set; } = string.Empty;

        public List<PgSchema> Schemas { get; set; } = new();
        public List<PgRole> Roles { get; set; } = new();   // ✅ new

    }

    public class PgSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;

        public CreateSchemaStmt Ast { get; set; }   // Parsed AST
        public string? AstJson { get; set; }    // Optional JSON representation of AST
        public List<PgTable> Tables { get; set; } = new();
        public List<PgView> Views { get; set; } = new();
        public List<PgFunction> Functions { get; set; } = new();
        public List<PgType> Types { get; set; } = new();
        public List<PgSequence> Sequences { get; set; } = new();
        public List<PgTrigger> Triggers { get; set; } = new();
    }

    public class PgTable
    {
        public string Name { get; set; }
        public string Definition { get; set; }   // Original SQL
        public CreateStmt? Ast  { get; set; }      // Parsed AST
        public string? AstJson { get; set; }    // Optional JSON representation of AST
        public string Owner { get; set; } = string.Empty;

        public List<PgColumn> Columns { get; set; } = new();
        public List<PgConstraint> Constraints { get; set; } = new();
        public List<PgIndex> Indexes { get; set; } = new();
    }

    public class PgColumn
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNotNull { get; set; }
        public string? DefaultExpression { get; set; }
    }

    public class PgConstraint
    {
        public string Name { get; set; }
        public ConstrType Type { get; set; }
        public List<string> Keys { get; set; } = new();
        public string? CheckExpression { get; set; }
        public string? ReferencedTable { get; set; }
        public List<string>? ReferencedColumns { get; set; }
    }

    public class PgIndex
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    public class PgView
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public string? Ast { get; set; }
        public string? AstJson { get; set; }    // Optional JSON representation of AST
        public string Owner { get; set; } = string.Empty;
    }

    public class PgFunction
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public string? Ast { get; set; }
        public string? AstJson { get; set; }    // Optional JSON representation of AST
        public string Owner { get; set; } = string.Empty;
    }

    public class PgType
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public CreateStmt? Ast { get; set; }
        public string? AstJson { get; set; }    // Optional JSON representation of AST
        public string Owner { get; set; } = string.Empty;
    }

    public class PgSequence
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public CreateSeqStmt? Ast { get; set; }
        public string? AstJson { get; set; }    // Optional JSON representation of AST
        public string Owner { get; set; } = string.Empty;
    }

    public class PgTrigger
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public string Ast { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    public class PgRole
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSuperUser { get; set; }
        public bool CanLogin { get; set; }
        public bool Inherit { get; set; }
        public bool Replication { get; set; }
        public bool BypassRLS { get; set; }
        public string? Password { get; set; }   // optional, often null
        public List<string> MemberOf { get; set; } = new(); // role memberships

        public string Definition { get; set; } = string.Empty; // CREATE ROLE SQL
    }

}
