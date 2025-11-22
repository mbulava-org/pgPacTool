using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using Npgquery.Native;
using PgQuery;
using System.IO.Compression;
using System.Text.Json;


namespace mbulava.PostgreSql.Dac.Extract
{
    

    public static class PgPackageLoader
    {
        public static PgProject LoadFromPackage(string packagePath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(packagePath, tempDir);

            // Read manifest
            var manifestPath = Path.Combine(tempDir, "manifest.json");
            var manifestJson = File.ReadAllText(manifestPath);
            var project = JsonSerializer.Deserialize<PgProject>(manifestJson)!;

            // Read objects
            var objectsDir = Path.Combine(tempDir, "objects");
            foreach (var file in Directory.GetFiles(objectsDir, "*.sql"))
            {
                var sql = File.ReadAllText(file);
                var parser = new Parser();
                var stmts = parser.Parse(sql);

                throw new NotImplementedException();
            //    foreach (var stmt in stmts)
            //    {
            //        switch (stmt)
            //        {
            //            case CreateStmt createTable:
            //                AddToSchema(project, createTable.Relation.Schemaname)
            //                    .Tables.Add(new PgTable(createTable.Relation.Relname, sql));
            //                break;

            //            case ViewStmt view:
            //                AddToSchema(project, view.View.Schemaname)
            //                    .Views.Add(new PgView(view.View.Relname, sql));
            //                break;

            //            case IndexStmt idx:
            //                AddToSchema(project, idx.Relation.Schemaname)
            //                    .Tables.First(t => t.Name == idx.Relation.Relname)
            //                    .Indexes.Add(new PgIndex(idx.Idxname, sql));
            //                break;

            //            case CreateFunctionStmt fn:
            //                AddToSchema(project, "public")
            //                    .Functions.Add(new PgFunction(fn.Funcname.First().ToString(), sql));
            //                break;

            //            case CreateTypeStmt ty:
            //                AddToSchema(project, "public")
            //                    .Types.Add(new PgType(ty.TypeName.First().ToString(), "public", "user", sql));
            //                break;

            //            case CreateSeqStmt seq:
            //                AddToSchema(project, "public")
            //                    .Sequences.Add(new PgSequence(seq.Sequence.Relname, "public", sql));
            //                break;

            //            case CreateTrigStmt trig:
            //                AddToSchema(project, "public")
            //                    .Triggers.Add(new PgTrigger(trig.Trigname, sql));
            //                break;
            //        }
            //    }
            }

            Directory.Delete(tempDir, true);
            return project;
        }

        private static PgSchema AddToSchema(PgProject project, string schemaName)
        {
            var schema = project.Schemas.FirstOrDefault(s => s.Name == schemaName);
            if (schema == null)
            {
                schema = new PgSchema { Name = schemaName };
                project.Schemas.Add(schema);
            }
            return schema;
        }
    }
}
