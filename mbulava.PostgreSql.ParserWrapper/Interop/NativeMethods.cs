using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.ParserWrapper.Interop
{
    internal static class NativeMethods
    {
        [DllImport("libpg_query", CallingConvention = CallingConvention.Cdecl)]
        public static extern PgQueryParseResult pg_query_parse(string input);

        [DllImport("libpg_query", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pg_query_free_parse_result(PgQueryParseResult result);
    }
}
