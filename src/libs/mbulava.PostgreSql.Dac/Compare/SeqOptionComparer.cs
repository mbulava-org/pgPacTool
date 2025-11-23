using mbulava.PostgreSql.Dac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compare
{
    public class SeqOptionComparer : IEqualityComparer<SeqOption>
    {
        public bool Equals(SeqOption? x, SeqOption? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return string.Equals(x.OptionName, y.OptionName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.OptionValue, y.OptionValue, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(SeqOption obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (obj.OptionName?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 23 + (obj.OptionValue?.ToLowerInvariant().GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
