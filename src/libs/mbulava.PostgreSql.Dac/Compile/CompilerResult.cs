using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compile
{
    public class CompilerResult
    {
        public List<CompilerError> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

}
