using mbulava.PostgreSql.ParserWrapper.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.ParserWrapper
{
    public static class PgQuery
    {
        public static string ParseToJson(string sql)
        {
            var result = NativeMethods.pg_query_parse(sql);

            if (result.error != IntPtr.Zero)
            {
                string error = Marshal.PtrToStringAnsi(result.error);
                throw new InvalidOperationException($"Parse error: {error}");
            }

            string json = Marshal.PtrToStringAnsi(result.parse_tree);
            NativeMethods.pg_query_free_parse_result(result);
            return json;
        }
    }
}
