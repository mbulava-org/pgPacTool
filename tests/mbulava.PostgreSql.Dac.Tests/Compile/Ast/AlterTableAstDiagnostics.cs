using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;
using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Diagnostic tests to examine ALTER TABLE AST structures.
/// Used to understand the JSON format for implementing pure AST builders.
/// </summary>
[TestFixture]
[Category("AstDiagnostics")]
public class AlterTableAstDiagnostics
{
    [Test]
    public void Debug_AlterTable_AddColumn_Structure()
    {
        var sql = "ALTER TABLE public.users ADD COLUMN email varchar(255) NOT NULL DEFAULT 'none';";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE ADD COLUMN AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_DropColumn_Structure()
    {
        var sql = "ALTER TABLE public.users DROP COLUMN IF EXISTS old_column;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE DROP COLUMN AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_AlterColumnType_Structure()
    {
        var sql = "ALTER TABLE public.users ALTER COLUMN age TYPE bigint;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE ALTER COLUMN TYPE AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_SetNotNull_Structure()
    {
        var sql = "ALTER TABLE public.users ALTER COLUMN email SET NOT NULL;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE SET NOT NULL AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_DropNotNull_Structure()
    {
        var sql = "ALTER TABLE public.users ALTER COLUMN email DROP NOT NULL;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE DROP NOT NULL AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_SetDefault_Structure()
    {
        var sql = "ALTER TABLE public.users ALTER COLUMN active SET DEFAULT true;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE SET DEFAULT AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_DropDefault_Structure()
    {
        var sql = "ALTER TABLE public.users ALTER COLUMN active DROP DEFAULT;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE DROP DEFAULT AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_AddConstraint_Structure()
    {
        var sql = "ALTER TABLE public.users ADD CONSTRAINT uk_email UNIQUE (email);";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE ADD CONSTRAINT AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_DropConstraint_Structure()
    {
        var sql = "ALTER TABLE public.users DROP CONSTRAINT IF EXISTS uk_email;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE DROP CONSTRAINT AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }

    [Test]
    public void Debug_AlterTable_Owner_Structure()
    {
        var sql = "ALTER TABLE public.users OWNER TO new_owner;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        TestContext.WriteLine("=== ALTER TABLE OWNER AST ===");
        TestContext.WriteLine(result.ParseTree!.RootElement.GetRawText());
    }
}
