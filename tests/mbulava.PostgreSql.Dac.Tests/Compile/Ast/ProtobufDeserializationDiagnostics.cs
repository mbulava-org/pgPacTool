using NUnit.Framework;
using Npgquery;
using PgQuery;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Diagnostic tests to understand why protobuf deserialization isn't working.
/// </summary>
[TestFixture]
[Category("Diagnostics")]
public class ProtobufDeserializationDiagnostics
{
    [Test]
    public void Investigate_ViewStmt_Properties()
    {
        // Check what properties ViewStmt actually has
        var viewStmtType = typeof(ViewStmt);
        
        TestContext.WriteLine("=== ViewStmt Type Information ===");
        TestContext.WriteLine($"Full Name: {viewStmtType.FullName}");
        TestContext.WriteLine($"Is Class: {viewStmtType.IsClass}");
        TestContext.WriteLine($"Is Public: {viewStmtType.IsPublic}");
        
        TestContext.WriteLine("\n=== Properties ===");
        var properties = viewStmtType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            var canRead = prop.CanRead;
            var canWrite = prop.CanWrite;
            var propType = prop.PropertyType.Name;
            
            TestContext.WriteLine($"Property: {prop.Name}");
            TestContext.WriteLine($"  Type: {propType}");
            TestContext.WriteLine($"  CanRead: {canRead}, CanWrite: {canWrite}");
            TestContext.WriteLine($"  JsonPropertyName: {jsonAttr?.Name ?? "None"}");
            TestContext.WriteLine();
        }
    }

    [Test]
    public void Investigate_SelectStmt_Properties()
    {
        var selectStmtType = typeof(SelectStmt);
        
        TestContext.WriteLine("=== SelectStmt Type Information ===");
        TestContext.WriteLine($"Full Name: {selectStmtType.FullName}");
        
        TestContext.WriteLine("\n=== Properties ===");
        var properties = selectStmtType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            TestContext.WriteLine($"Property: {prop.Name} (JsonName: {jsonAttr?.Name ?? "None"})");
        }
    }

    [Test]
    public void Test_ViewStmt_Deserialization_CaseInsensitive()
    {
        var sql = @"CREATE VIEW public.test_view AS SELECT id FROM public.test_table;";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        var root = result.ParseTree!.RootElement;
        
        // Get the ViewStmt JSON
        root.TryGetProperty("stmts", out var stmts);
        var stmt = stmts[0];
        stmt.TryGetProperty("stmt", out var stmtObj);
        stmtObj.TryGetProperty("ViewStmt", out var viewStmtJson);
        
        var json = viewStmtJson.GetRawText();
        TestContext.WriteLine("=== Raw JSON ===");
        TestContext.WriteLine(json);
        
        // Test 1: Default deserialization
        TestContext.WriteLine("\n=== Test 1: Default Deserialization ===");
        try
        {
            var viewStmt1 = JsonSerializer.Deserialize<ViewStmt>(json);
            TestContext.WriteLine($"Success: {viewStmt1 != null}");
            TestContext.WriteLine($"View: {viewStmt1?.View}");
            TestContext.WriteLine($"Query: {viewStmt1?.Query}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed: {ex.Message}");
        }
        
        // Test 2: Case insensitive
        TestContext.WriteLine("\n=== Test 2: PropertyNameCaseInsensitive ===");
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var viewStmt2 = JsonSerializer.Deserialize<ViewStmt>(json, options);
            TestContext.WriteLine($"Success: {viewStmt2 != null}");
            TestContext.WriteLine($"View: {viewStmt2?.View}");
            TestContext.WriteLine($"Query: {viewStmt2?.Query}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed: {ex.Message}");
        }
        
        // Test 3: CamelCase policy
        TestContext.WriteLine("\n=== Test 3: CamelCase Naming Policy ===");
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            var viewStmt3 = JsonSerializer.Deserialize<ViewStmt>(json, options);
            TestContext.WriteLine($"Success: {viewStmt3 != null}");
            TestContext.WriteLine($"View: {viewStmt3?.View}");
            TestContext.WriteLine($"Query: {viewStmt3?.Query}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed: {ex.Message}");
        }
        
        // Test 4: Manual mapping test
        TestContext.WriteLine("\n=== Test 4: Check JSON Property Names ===");
        var jsonDoc = JsonDocument.Parse(json);
        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            TestContext.WriteLine($"JSON Property: '{property.Name}'");
        }
    }

    [Test]
    public void Test_CreateStmt_Deserialization()
    {
        var sql = @"
            CREATE TABLE public.orders (
                id integer PRIMARY KEY,
                customer_id integer REFERENCES public.customers(id)
            );";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        var root = result.ParseTree!.RootElement;
        
        root.TryGetProperty("stmts", out var stmts);
        var stmt = stmts[0];
        stmt.TryGetProperty("stmt", out var stmtObj);
        stmtObj.TryGetProperty("CreateStmt", out var createStmtJson);
        
        var json = createStmtJson.GetRawText();
        
        TestContext.WriteLine("=== Test CreateStmt Deserialization ===");
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var createStmt = JsonSerializer.Deserialize<CreateStmt>(json, options);
            TestContext.WriteLine($"Success: {createStmt != null}");
            TestContext.WriteLine($"Relation: {createStmt?.Relation}");
            TestContext.WriteLine($"TableElts: {createStmt?.TableElts?.Count ?? 0}");
            TestContext.WriteLine($"InhRelations: {createStmt?.InhRelations?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed: {ex.Message}");
        }
    }

    [Test]
    public void Test_CreateTrigStmt_Deserialization()
    {
        var sql = @"
            CREATE TRIGGER audit_trigger
            AFTER INSERT OR UPDATE ON public.customers
            FOR EACH ROW
            EXECUTE FUNCTION public.audit_changes();";
        
        using var parser = new Parser();
        var result = parser.Parse(sql);
        var root = result.ParseTree!.RootElement;
        
        root.TryGetProperty("stmts", out var stmts);
        var stmt = stmts[0];
        stmt.TryGetProperty("stmt", out var stmtObj);
        stmtObj.TryGetProperty("CreateTrigStmt", out var trigStmtJson);
        
        var json = trigStmtJson.GetRawText();
        
        TestContext.WriteLine("=== Test CreateTrigStmt Deserialization ===");
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var trigStmt = JsonSerializer.Deserialize<CreateTrigStmt>(json, options);
            TestContext.WriteLine($"Success: {trigStmt != null}");
            TestContext.WriteLine($"Trigname: {trigStmt?.Trigname}");
            TestContext.WriteLine($"Relation: {trigStmt?.Relation}");
            TestContext.WriteLine($"Funcname: {trigStmt?.Funcname?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Failed: {ex.Message}");
        }
    }

    [Test]
    public void Compare_JsonElement_vs_Protobuf_Consistency()
    {
        TestContext.WriteLine("=== Testing JSON Format Consistency ===\n");
        
        var testCases = new[]
        {
            ("Simple View", @"CREATE VIEW v AS SELECT * FROM t;"),
            ("View with JOIN", @"CREATE VIEW v AS SELECT * FROM t1 JOIN t2 ON t1.id = t2.id;"),
            ("View with CTE", @"CREATE VIEW v AS WITH cte AS (SELECT * FROM t) SELECT * FROM cte;"),
            ("Table with FK", @"CREATE TABLE t (id int, fk int REFERENCES other(id));"),
            ("Table with Inheritance", @"CREATE TABLE t () INHERITS (parent);"),
            ("Trigger", @"CREATE TRIGGER trig AFTER INSERT ON t FOR EACH ROW EXECUTE FUNCTION f();")
        };

        foreach (var (name, sql) in testCases)
        {
            TestContext.WriteLine($"--- {name} ---");
            try
            {
                using var parser = new Parser();
                var result = parser.Parse(sql);
                
                if (result.IsSuccess && result.ParseTree != null)
                {
                    var root = result.ParseTree.RootElement;
                    TestContext.WriteLine($"✓ Parses successfully");
                    
                    // Check structure consistency
                    if (root.TryGetProperty("stmts", out var stmts))
                    {
                        TestContext.WriteLine($"✓ Has 'stmts' property");
                        
                        if (stmts.GetArrayLength() > 0)
                        {
                            var firstStmt = stmts[0];
                            if (firstStmt.TryGetProperty("stmt", out var stmt))
                            {
                                TestContext.WriteLine($"✓ Has 'stmt' property");
                                
                                // List statement types
                                TestContext.Write("  Statement types: ");
                                foreach (var prop in stmt.EnumerateObject())
                                {
                                    TestContext.Write($"{prop.Name} ");
                                }
                                TestContext.WriteLine();
                            }
                        }
                    }
                }
                else
                {
                    TestContext.WriteLine($"✗ Failed to parse: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"✗ Exception: {ex.Message}");
            }
            TestContext.WriteLine();
        }
    }
}
