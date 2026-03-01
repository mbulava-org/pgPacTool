using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

[TestFixture]
[Category("AstDebug")]
public class GrantRevokeDiagnostics
{
    [Test]
    public void Debug_Grant_SELECT()
    {
        try
        {
            var ast = AstBuilder.Grant("SELECT", "TABLE", "public", "users", "app_user");
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
