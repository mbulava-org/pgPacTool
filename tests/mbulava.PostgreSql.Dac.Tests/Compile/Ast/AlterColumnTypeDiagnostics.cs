using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

[TestFixture]
[Category("AstDebug")]
public class AlterColumnTypeDiagnostics
{
    [Test]
    public void Debug_AlterColumnType_BIGINT()
    {
        try
        {
            // Try to build AST for BIGINT type
            var ast = AstBuilder.AlterTableAlterColumnType("public", "users", "age", "BIGINT");
            
            // Generate SQL
            var sql = AstSqlGenerator.Generate(ast);
            
            TestContext.WriteLine("=== Generated SQL ===");
            TestContext.WriteLine(sql);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine("=== ERROR ===");
            TestContext.WriteLine(ex.ToString());
            throw;
        }
    }
}
