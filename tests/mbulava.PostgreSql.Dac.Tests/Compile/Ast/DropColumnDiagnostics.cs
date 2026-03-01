using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

[TestFixture]
[Category("AstDebug")]
public class DropColumnDiagnostics
{
    [Test]
    public void Debug_DropColumn_Output()
    {
        // Build AST
        var ast = AstBuilder.AlterTableDropColumn("public", "users", "old_column", ifExists: true);
        
        // Generate SQL
        var sql = AstSqlGenerator.Generate(ast);
        
        TestContext.WriteLine("=== Generated SQL ===");
        TestContext.WriteLine(sql);
        
        // Also check what parsing "DROP COLUMN" produces
        var expectedSql = "ALTER TABLE public.users DROP COLUMN IF EXISTS old_column;";
        using var doc = AstSqlGenerator.ParseToAst(expectedSql);
        var roundTrip = AstSqlGenerator.Generate(doc);
        
        TestContext.WriteLine("\n=== Round-trip SQL ===");
        TestContext.WriteLine(roundTrip);
    }
}
