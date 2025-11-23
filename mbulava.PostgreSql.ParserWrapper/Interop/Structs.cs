using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.ParserWrapper.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PgQueryParseResult
    {
        public IntPtr parse_tree;
        public IntPtr stderr_buffer;
        public IntPtr error;
    }
}
