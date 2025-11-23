using mbulava.PostgreSql.Dac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compare
{
    public class PgAttributeComparer : IEqualityComparer<PgAttribute>
    {
        public bool Equals(PgAttribute? x, PgAttribute? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.DataType, y.DataType, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(PgAttribute obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (obj.Name?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 23 + (obj.DataType?.ToLowerInvariant().GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
