using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compile
{
    public class CompilerError
    {
        public string File { get; }
        public string Message { get; }
        public string Detail { get; }

        public CompilerError(string file, string message, string detail)
        {
            File = file;
            Message = message;
            Detail = detail;
        }
    }

}
