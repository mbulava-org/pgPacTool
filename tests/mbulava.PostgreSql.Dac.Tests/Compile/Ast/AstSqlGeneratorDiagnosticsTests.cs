using NUnit.Framework;
using mbulava.PostgreSql.Dac.Compile.Ast;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Diagnostic tests to debug cross-platform AST SQL generation issues
/// </summary>
[TestFixture]
[Category("Diagnostics")]
public class AstSqlGeneratorDiagnosticsTests
{
    [Test]
    public void Diagnostic_AlterColumnType_ShowsGeneratedSql()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAlterColumnType("public", "users", "age", "INT");

        // Act
        TestContext.WriteLine("=== AST JSON ===");
        TestContext.WriteLine(ast.GetRawText());
        TestContext.WriteLine();

        string sql;
        try
        {
            sql = AstSqlGenerator.Generate(ast);
            TestContext.WriteLine("=== Generated SQL ===");
            TestContext.WriteLine(sql);
            TestContext.WriteLine();

            // Check for garbage characters
            var hasGarbage = sql.Any(c => c < 0x20 && c != '\n' && c != '\r' && c != '\t');
            TestContext.WriteLine($"Has garbage characters: {hasGarbage}");
            
            if (hasGarbage)
            {
                TestContext.WriteLine("=== Hex dump of first 100 chars ===");
                var chars = sql.Take(100).ToArray();
                foreach (var c in chars)
                {
                    TestContext.WriteLine($"  '{c}' = 0x{((int)c):X2}");
                }
            }

            // Assert
            Assert.That(hasGarbage, Is.False, "Generated SQL should not contain garbage characters");
            Assert.That(sql.ToUpper(), Does.Contain("ALTER"), "SQL should contain ALTER keyword");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"=== EXCEPTION ===");
            TestContext.WriteLine($"Type: {ex.GetType().Name}");
            TestContext.WriteLine($"Message: {ex.Message}");
            TestContext.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    [Test]
    public void Diagnostic_DropColumn_ShowsGeneratedSql()
    {
        // Arrange
        var ast = AstBuilder.AlterTableDropColumn("public", "users", "old_column", ifExists: true);

        // Act
        TestContext.WriteLine("=== AST JSON ===");
        TestContext.WriteLine(ast.GetRawText());
        TestContext.WriteLine();

        string sql;
        try
        {
            sql = AstSqlGenerator.Generate(ast);
            TestContext.WriteLine("=== Generated SQL ===");
            TestContext.WriteLine(sql);
            TestContext.WriteLine();

            // Check for garbage characters
            var hasGarbage = sql.Any(c => c < 0x20 && c != '\n' && c != '\r' && c != '\t');
            TestContext.WriteLine($"Has garbage characters: {hasGarbage}");

            // Assert
            Assert.That(hasGarbage, Is.False, "Generated SQL should not contain garbage characters");
            Assert.That(sql.ToUpper(), Does.Contain("DROP"), "SQL should contain DROP keyword");
            Assert.That(sql.ToUpper(), Does.Contain("OLD_COLUMN"), "SQL should reference the column name");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"=== EXCEPTION ===");
            TestContext.WriteLine($"Type: {ex.GetType().Name}");
            TestContext.WriteLine($"Message: {ex.Message}");
            TestContext.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    [Test]
    [Ignore("Deparse uses protobuf - broken on Linux. See Issue #36")]
    public void Diagnostic_ProtobufDeparse_ShowsRawOutput()
    {
        // This test helps diagnose if the issue is in protobuf conversion
        // Arrange
        var testSql = "ALTER TABLE users ALTER COLUMN age TYPE INT;";
        
        TestContext.WriteLine($"=== Original SQL ===");
        TestContext.WriteLine(testSql);
        TestContext.WriteLine();

        try
        {
            using var parser = new Npgquery.Parser();
            
            // Parse to get AST
            var parseResult = parser.Parse(testSql);
            Assert.That(parseResult.IsSuccess, Is.True, "Parse should succeed");
            
            TestContext.WriteLine("=== AST JSON ===");
            TestContext.WriteLine(parseResult.ParseTree?.RootElement.ToString());
            TestContext.WriteLine();

            // Try to deparse
            var deparseResult = parser.Deparse(parseResult.ParseTree!);
            
            TestContext.WriteLine("=== Deparse Result ===");
            TestContext.WriteLine($"Success: {deparseResult.IsSuccess}");
            TestContext.WriteLine($"Error: {deparseResult.Error}");
            TestContext.WriteLine($"Query: {deparseResult.Query}");
            TestContext.WriteLine();

            if (!string.IsNullOrEmpty(deparseResult.Query))
            {
                var hasGarbage = deparseResult.Query.Any(c => c < 0x20 && c != '\n' && c != '\r' && c != '\t');
                TestContext.WriteLine($"Has garbage: {hasGarbage}");
                
                if (hasGarbage)
                {
                    TestContext.WriteLine("=== First 50 characters (hex) ===");
                    var chars = deparseResult.Query.Take(50).ToArray();
                    for (int i = 0; i < chars.Length; i++)
                    {
                        TestContext.WriteLine($"  [{i}] '{chars[i]}' = 0x{((int)chars[i]):X2}");
                    }
                }
            }

            // Assert - on Windows this should work, on Linux it might fail
            Assert.That(deparseResult.IsSuccess, Is.True, "Deparse should succeed");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"=== EXCEPTION ===");
            TestContext.WriteLine($"Type: {ex.GetType().Name}");
            TestContext.WriteLine($"Message: {ex.Message}");
            throw;
        }
    }
}
