using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using PgQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compile
{
    public class PgCompiler
    {
        private readonly string _projectPath;

        public PgCompiler(string projectPath)
        {
            _projectPath = projectPath;
        }

        public async Task<PgProject> CompileAsync()
        {
            var sqlFiles = DiscoverSqlFiles(_projectPath);

            var project = new PgProject
            {
                DatabaseName = Path.GetFileNameWithoutExtension(_projectPath),
                PostgresVersion = "16" // or detect dynamically
            };

            foreach (var file in sqlFiles)
            {
                var sql = await File.ReadAllTextAsync(file);

                var parser = new Parser();
                var result = parser.Parse(sql);

                if (!result.IsValid)
                    throw new InvalidOperationException($"Invalid SQL in {file}: {result.ErrorMessage}");

                foreach (var stmt in result.Stmts)
                {
                    switch (stmt)
                    {
                        case CreateSchemaStmt schemaStmt:
                            project.Schemas.Add(MapSchema(schemaStmt, result.ParseTree));
                            break;

                        case CreateStmt tableStmt:
                            AddTable(project, tableStmt, result.ParseTree);
                            break;

                        case CreateTypeStmt typeStmt:
                            AddType(project, typeStmt, result.ParseTree);
                            break;

                        case CreateSeqStmt seqStmt:
                            AddSequence(project, seqStmt, result.ParseTree);
                            break;

                        case CreateTrigStmt trigStmt:
                            AddTrigger(project, trigStmt, result.ParseTree);
                            break;

                        default:
                            throw new InvalidOperationException($"Unsupported statement in {file}: {stmt.Tag}");
                    }
                }
            }

            return project;
        }

        private IEnumerable<string> DiscoverSqlFiles(string projectPath)
        {
            var allFiles = Directory.EnumerateFiles(Path.GetDirectoryName(projectPath)!, "*.sql", SearchOption.AllDirectories);

            return allFiles.Where(f =>
                !f.Contains("PreDeployment", StringComparison.OrdinalIgnoreCase) &&
                !f.Contains("PostDeployment", StringComparison.OrdinalIgnoreCase) &&
                !f.Contains(Path.Combine("bin", ""), StringComparison.OrdinalIgnoreCase) &&
                !f.Contains(Path.Combine("obj", ""), StringComparison.OrdinalIgnoreCase) &&
                !f.Contains(Path.Combine("Common", ""), StringComparison.OrdinalIgnoreCase));
        }

        private PgSchema MapSchema(CreateSchemaStmt stmt, string astJson)
        {
            return new PgSchema
            {
                Name = stmt.Schemaname,
                Owner = stmt.Authid ?? "postgres",
                Ast = stmt,
                AstJson = astJson
            };
        }

        private void AddTable(PgProject project, CreateStmt stmt, string astJson)
        {
            var schema = project.Schemas.FirstOrDefault(s => s.Name == stmt.Relation.Schemaname);
            if (schema == null)
            {
                schema = new PgSchema { Name = stmt.Relation.Schemaname, Owner = "postgres" };
                project.Schemas.Add(schema);
            }

            schema.Tables.Add(new PgTable
            {
                Name = stmt.Relation.Relname,
                Definition = stmt.ToString(),
                Ast = stmt,
                AstJson = astJson,
                Owner = "postgres" // can be refined later
            });
        }

        private void AddType(PgProject project, CreateTypeStmt stmt, string astJson)
        {
            var schema = project.Schemas.FirstOrDefault(s => s.Name == stmt.TypeName.First());
            if (schema == null)
            {
                schema = new PgSchema { Name = stmt.TypeName.First(), Owner = "postgres" };
                project.Schemas.Add(schema);
            }

            schema.Types.Add(new PgType
            {
                Name = stmt.TypeName.Last(),
                Definition = stmt.ToString(),
                Ast = stmt,
                AstJson = astJson,
                Owner = "postgres"
            });
        }

        private void AddSequence(PgProject project, CreateSeqStmt stmt, string astJson)
        {
            var schema = project.Schemas.FirstOrDefault(s => s.Name == stmt.Sequence.Schemaname);
            if (schema == null)
            {
                schema = new PgSchema { Name = stmt.Sequence.Schemaname, Owner = "postgres" };
                project.Schemas.Add(schema);
            }

            schema.Sequences.Add(new PgSequence
            {
                Name = stmt.Sequence.Relname,
                Definition = stmt.ToString(),
                Ast = stmt,
                AstJson = astJson,
                Owner = "postgres"
            });
        }

        private void AddTrigger(PgProject project, CreateTrigStmt stmt, string astJson)
        {
            var schema = project.Schemas.FirstOrDefault(s => s.Name == stmt.Relation.Schemaname);
            if (schema == null)
            {
                schema = new PgSchema { Name = stmt.Relation.Schemaname, Owner = "postgres" };
                project.Schemas.Add(schema);
            }

            schema.Triggers.Add(new PgTrigger
            {
                Name = stmt.Trigname,
                Definition = stmt.ToString(),
                Ast = stmt,
                AstJson = astJson,
                Owner = "postgres"
            });
        }
    }
}
