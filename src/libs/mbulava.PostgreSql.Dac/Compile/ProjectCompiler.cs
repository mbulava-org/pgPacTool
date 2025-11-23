//using mbulava.PostgreSql.Dac.Models;
//using Npgquery;
//using PgQuery;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

using Npgquery;
using PgQuery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile
{
    public class ProjectCompiler
    {
        private readonly string _projectPath;
        private readonly Parser _parser = new Parser();

        public ProjectCompiler(string projectPath)
        {
            _projectPath = projectPath;
        }



        public CompilerResult Validate()
        {
            var result = new CompilerResult();

            foreach (var file in Directory.EnumerateFiles(_projectPath, "*.sql", SearchOption.AllDirectories))
            {
                var sql = File.ReadAllText(file);

                // 1. Parse
                var parseResult = _parser.Parse(sql);
                if (parseResult.IsError)
                {
                    result.Errors.Add(new CompilerError(file, "Parse error", parseResult.Error));
                    continue;
                }


                // The "stmts" array is where all statements live
                var stmts = parseResult.ParseTree.RootElement.GetProperty("stmts");


                // 2. Enforce Create Only
                foreach (var stmt in stmts.EnumerateArray())
                {
                    var node = JsonSerializer.Deserialize<Node>(stmt.GetProperty("stmt"));

                    if (!IsCreateOnly(node))
                    {
                        result.Errors.Add(new CompilerError(file, "Non-create statement found", stmt.GetRawText()));
                    }
                }

                // 3. Reference validation
                //foreach (var reference in ExtractReferences(parseResult))
                //{
                //    if (!ReferenceExists(reference))
                //    {
                //        result.Errors.Add(new CompilerError(file, "Missing reference", reference));
                //    }
                //}
            }

            return result;
        }

        private bool IsCreateOnly(Node stmt)
        {
            return stmt is CreateStmt || stmt is CreateSeqStmt || stmt is CreateDomainStmt || stmt is CreateEnumStmt || stmt is CompositeTypeStmt;
        }

        //private IEnumerable<string> ExtractReferences(ParseResult parseResult)
        //{
        //    // Walk AST to collect referenced object names
        //    // e.g. foreign keys, sequence nextval, type usage
        //    return new List<string>();
        //}

        private bool ReferenceExists(string reference)
        {
            // Check against project catalog or target schema
            return true;
        }
    }

    
}
