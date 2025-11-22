using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Models
{

    public class ObjectDiff
    {
        public string ObjectType { get; set; } = string.Empty; // Table, Schema, Index, Role, etc.
        public string ObjectName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;     // Create, Drop, Alter
        public string Script { get; set; } = string.Empty;     // SQL to apply
    }


}
