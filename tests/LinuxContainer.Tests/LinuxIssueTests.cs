using FluentAssertions;

namespace LinuxContainer.Tests;

/// <summary>
/// Focused tests for specific Linux issues discovered during development.
/// These tests reproduce known Linux-specific bugs to ensure fixes work correctly.
/// </summary>
[TestFixture]
[Category("LinuxContainer")]
[Category("RequiresDocker")]
public class LinuxIssueTests : LinuxContainerTestBase
{
    [Test]
    public async Task ProtobufDeparse_ShouldGenerateValidSQL_NotGarbage()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var script = @"
#!/bin/bash
set -e

echo ""Testing Protobuf Deparse Issue (GitHub Issue #X)""
echo ""=============================================""

# Build the DAC library
cd src/libs/mbulava.PostgreSql.Dac
dotnet build -c Release --no-restore

# Run only the protobuf-related tests
cd ../../..
dotnet test tests/mbulava.PostgreSql.Dac.Tests/mbulava.PostgreSql.Dac.Tests.csproj \
    -c Release \
    --no-build \
    --no-restore \
    --filter ""FullyQualifiedName~PublishScriptGeneratorTests"" \
    --logger ""console;verbosity=detailed""

echo """"
echo ""✅ Protobuf deparse tests passed on Linux""
";

        var result = await RunScriptInLinuxContainerAsync("protobuf-test", script);

        result.ExitCode.Should().Be(0, because: "protobuf deparse should work correctly on Linux");
        result.Output.Should().NotContain(@"\u0012", because: "output should not contain protobuf control characters");
        result.Output.Should().NotContain(@"\u0006", because: "output should not contain protobuf field markers");
        result.Output.Should().Contain("ALTER", because: "generated SQL should contain ALTER statements");
        result.Output.Should().Contain("GRANT", because: "generated SQL should contain GRANT statements");
    }

    [Test]
    public async Task NativeLibrary_ShouldLoadWithoutErrors_OnLinux()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var script = @"
#!/bin/bash
set -e

echo ""Testing Native Library Loading on Linux""
echo ""========================================""

# Check libpg_query.so exists and is valid
NATIVE_LIB=""src/libs/Npgquery/Npgquery/runtimes/linux-x64/native/libpg_query.so""

if [ ! -f ""$NATIVE_LIB"" ]; then
    echo ""❌ ERROR: $NATIVE_LIB not found""
    exit 1
fi

echo ""✅ Found: $NATIVE_LIB""
ls -lh ""$NATIVE_LIB""

# Verify it's a valid ELF binary
file ""$NATIVE_LIB""

# Check dependencies
echo """"
echo ""Checking library dependencies:""
ldd ""$NATIVE_LIB"" 2>&1 || true

# Build and run Npgquery tests
echo """"
echo ""Running Npgquery tests to verify library loads:""
cd src/libs/Npgquery/Npgquery.Tests
dotnet build -c Release --no-restore

# Run a simple parse test to ensure library loads
dotnet test -c Release --no-build --no-restore \
    --filter ""FullyQualifiedName~BasicParse"" \
    --logger ""console;verbosity=detailed"" || exit 1

echo """"
echo ""✅ Native library loads and works correctly on Linux""
";

        var result = await RunScriptInLinuxContainerAsync("native-lib-test", script);

        result.ExitCode.Should().Be(0, because: "native library should load without errors");
        result.Output.Should().Contain("ELF 64-bit", because: "it should be a valid Linux binary");
        result.Output.Should().NotContain("error while loading shared libraries", because: "all dependencies should be satisfied");
    }

    [Test]
    public async Task AstSqlGenerator_ShouldUseJsonExtraction_NotProtobuf()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var script = @"
#!/bin/bash
set -e

echo ""Testing AST SQL Generator JSON Extraction""
echo ""=========================================""

# Create a test program to verify the fix
cat > /tmp/test_ast.cs << 'TESTCODE'
using System;
using System.Text.Json;
using mbulava.PostgreSql.Dac.Compile.Ast;

class Program
{
    static void Main()
    {
        // Test data: Simple ALTER TABLE statement AST
        var astJson = @""{
            \""stmts\"": [{
                \""stmt\"": {
                    \""AlterTableStmt\"": {
                        \""relation\"": {
                            \""RangeVar\"": {
                                \""schemaname\"": \""public\"",
                                \""relname\"": \""users\""
                            }
                        },
                        \""cmds\"": [{
                            \""AlterTableCmd\"": {
                                \""subtype\"": \""AT_DropColumn\"",
                                \""name\"": \""old_column\"",
                                \""missing_ok\"": true
                            }
                        }]
                    }
                }
            }]
        }"";

        using var doc = JsonDocument.Parse(astJson);
        var sql = AstSqlGenerator.Generate(doc);
        
        Console.WriteLine(""Generated SQL: "" + sql);
        
        // Verify no protobuf garbage
        if (sql.Contains(""\u0012"") || sql.Contains(""\u0006""))
        {
            Console.WriteLine(""❌ FAIL: Output contains protobuf garbage"");
            Environment.Exit(1);
        }
        
        // Verify correct SQL
        if (!sql.ToUpper().Contains(""DROP COLUMN""))
        {
            Console.WriteLine(""❌ FAIL: Missing DROP COLUMN in output"");
            Environment.Exit(1);
        }
        
        Console.WriteLine(""✅ PASS: SQL generated correctly using JSON extraction"");
    }
}
TESTCODE

# Compile the test
cd /workspace
dotnet new console -n AstTest -o /tmp/AstTest --force
cd /tmp/AstTest
dotnet add reference /workspace/src/libs/mbulava.PostgreSql.Dac/mbulava.PostgreSql.Dac.csproj
cp /tmp/test_ast.cs Program.cs
dotnet build -c Release

# Run the test
dotnet run -c Release

echo """"
echo ""✅ AST SQL Generator works correctly on Linux""
";

        var result = await RunScriptInLinuxContainerAsync("ast-json-test", script);

        result.ExitCode.Should().Be(0, because: "JSON extraction should work correctly");
        result.Output.Should().Contain("✅ PASS", because: "test validation should pass");
        result.Output.Should().Contain("DROP COLUMN", because: "correct SQL should be generated");
        result.Output.Should().NotContain("protobuf garbage", because: "no protobuf corruption should occur");
    }

    [Test]
    [Explicit("Slow test - verifies complete CI simulation")]
    public async Task CompleteCI_Simulation_AllTestsShouldPass()
    {
        if (!IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Install Docker Desktop to run this test.");
            return;
        }

        var script = @"
#!/bin/bash
set -e

echo ""========================================""
echo ""Complete CI Simulation""
echo ""This mimics GitHub Actions workflow""
echo ""========================================""

# Show environment (like GitHub Actions does)
echo """"
echo ""Environment Information:""
echo ""----------------------""
dotnet --info
uname -a
cat /etc/os-release | head -5

# Restore solution
echo """"
echo ""Restoring solution...""
dotnet restore

# Build solution
echo """"
echo ""Building solution...""
dotnet build -c Release --no-restore

# Run all tests
echo """"
echo ""Running all tests...""
dotnet test -c Release --no-build --no-restore --logger ""console;verbosity=normal""

# Check for specific failures
echo """"
echo ""Verification:""
if dotnet test -c Release --no-build --no-restore \
    --filter ""FullyQualifiedName~PublishScriptGeneratorTests"" 2>&1 | grep -q ""Failed:""; then
    echo ""❌ PublishScriptGeneratorTests failed""
    exit 1
else
    echo ""✅ PublishScriptGeneratorTests passed""
fi

echo """"
echo ""========================================""
echo ""✅ Complete CI simulation succeeded""
echo ""========================================""
";

        var result = await RunScriptInLinuxContainerAsync("complete-ci-sim", script);

        result.ExitCode.Should().Be(0, because: "all tests should pass in complete CI simulation");
        result.Output.Should().Contain("✅ Complete CI simulation succeeded");
        result.Output.Should().NotContain("❌");
        result.Output.Should().NotContain(@"\u0012", because: "no protobuf corruption should occur");
    }
}
