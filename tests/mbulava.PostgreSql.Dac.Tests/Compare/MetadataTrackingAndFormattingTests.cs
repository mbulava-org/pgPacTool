using FluentAssertions;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Tests.Compare;

[TestFixture]
public class MetadataTrackingAndFormattingTests
{
    [Test]
    public void PublishScriptGenerator_WithTrackDeploymentMetadata_GeneratesTableAndUpserts()
    {
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            FunctionDiffs = new List<PgFunctionDiff>
            {
                new()
                {
                    FunctionName = "calculate_total",
                    SourceDefinition = "CREATE FUNCTION public.calculate_total(p_id integer, p_discount numeric DEFAULT NULL) RETURNS numeric AS $$ BEGIN RETURN 100; END; $$ LANGUAGE plpgsql;",
                    SourceFilePath = "public/Functions/calculate_total.sql",
                    DefinitionChanged = true
                }
            },
            TypeDiffs = new List<PgTypeDiff>
            {
                new()
                {
                    TypeName = "item_type",
                    SourceDefinition = "CREATE TYPE public.item_type AS (\n    id integer,\n    name text\n);",
                    SourceFilePath = "public/Types/item_type.sql",
                    DefinitionChanged = true
                }
            }
        };

        var options = new PublishOptions
        {
            TrackDeploymentMetadata = true,
            MetadataSchema = "public",
            MetadataTableName = "__pgpac_objects",
            IncludeComments = false
        };

        var script = PublishScriptGenerator.Generate(diff, options);

        script.Should().Contain("CREATE TABLE IF NOT EXISTS \"public\".\"__pgpac_objects\"");
        script.Should().Contain("INSERT INTO \"public\".\"__pgpac_objects\"");
        script.Should().Contain("'calculate_total'");
        script.Should().Contain("'FUNCTION'");
        script.Should().Contain("'public/Functions/calculate_total.sql'");
        script.Should().Contain("'item_type'");
        script.Should().Contain("'TYPE'");
        script.Should().Contain("'public/Types/item_type.sql'");
    }

    [Test]
    public void PublishScriptGenerator_WithoutTrackDeploymentMetadata_OmitsMetadataSql()
    {
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            FunctionDiffs = new List<PgFunctionDiff>
            {
                new()
                {
                    FunctionName = "calculate_total",
                    SourceDefinition = "CREATE FUNCTION public.calculate_total() RETURNS void AS $$ BEGIN END; $$ LANGUAGE plpgsql;",
                    SourceFilePath = "public/Functions/calculate_total.sql",
                    DefinitionChanged = true
                }
            }
        };

        var options = new PublishOptions
        {
            TrackDeploymentMetadata = false,
            IncludeComments = false
        };

        var script = PublishScriptGenerator.Generate(diff, options);

        script.Should().NotContain("__pgpac_objects");
        script.Should().NotContain("INSERT INTO");
    }

    [Test]
    public void PublishScriptGenerator_DropObjects_GeneratesMetadataDelete()
    {
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            FunctionDiffs = new List<PgFunctionDiff>
            {
                new()
                {
                    FunctionName = "old_function",
                    SourceDefinition = null,
                    TargetDefinition = "CREATE FUNCTION public.old_function() RETURNS void AS $$ BEGIN END; $$ LANGUAGE plpgsql;",
                    DefinitionChanged = true
                }
            }
        };

        var options = new PublishOptions
        {
            TrackDeploymentMetadata = true,
            DropObjectsNotInSource = true,
            IncludeComments = false
        };

        var script = PublishScriptGenerator.Generate(diff, options);

        script.Should().Contain("DELETE FROM \"public\".\"__pgpac_objects\" WHERE schema_name = 'public' AND object_name = 'old_function' AND object_type = 'FUNCTION';");
    }

    [Test]
    public void PgSchemaComparer_SemanticFunctionComparison_IdentifiesEquivalentFunctionsWithDefaultNullCasts()
    {
        var srcFuncSql = @"CREATE FUNCTION public.test_func(p_name varchar, p_val int = NULL)
RETURNS boolean
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN true;
END;
$function$;";

        var tgtFuncSql = @"CREATE FUNCTION public.test_func(p_name character varying, p_val integer DEFAULT NULL::integer)
RETURNS boolean
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN true;
END;
$$;";

        var areEqual = PgSchemaComparer.AreFunctionsEqual(srcFuncSql, tgtFuncSql);
        areEqual.Should().BeTrue();

        var sourceSchema = new PgSchema
        {
            Name = "public",
            Functions = new List<PgFunction>
            {
                new()
                {
                    Name = "test_func",
                    Definition = srcFuncSql,
                    SourceFilePath = "public/Functions/test_func.sql"
                }
            }
        };

        var targetSchema = new PgSchema
        {
            Name = "public",
            Functions = new List<PgFunction>
            {
                new()
                {
                    Name = "test_func",
                    Definition = tgtFuncSql
                }
            }
        };

        var comparer = new PgSchemaComparer();
        var diff = comparer.Compare(sourceSchema, targetSchema, new CompareOptions());

        diff.FunctionDiffs.Should().BeEmpty();
    }

    [Test]
    public void PgSchemaComparer_IgnoresInternalMetadataTable()
    {
        var sourceSchema = new PgSchema
        {
            Name = "public",
            Tables = new List<PgTable>
            {
                new() { Name = "users", Definition = "CREATE TABLE public.users (id int);" }
            }
        };

        var targetSchema = new PgSchema
        {
            Name = "public",
            Tables = new List<PgTable>
            {
                new() { Name = "users", Definition = "CREATE TABLE public.users (id int);" },
                new() { Name = "__pgpac_objects", Definition = "CREATE TABLE public.__pgpac_objects (...);" }
            }
        };

        var comparer = new PgSchemaComparer();
        var diff = comparer.Compare(sourceSchema, targetSchema, new CompareOptions());

        diff.TableDiffs.Should().BeEmpty();
    }

    [Test]
    public void PgProjectExtractor_CleanFunctionDecompiledDefinition_StripsRedundantNullCasts()
    {
        var decompiledSql = "CREATE FUNCTION public.foo(a character varying DEFAULT NULL::character varying, b integer DEFAULT NULL::integer, c text DEFAULT ''::text) RETURNS void AS $$ BEGIN END; $$ LANGUAGE plpgsql;";
        var cleaned = PgProjectExtractor.CleanFunctionDecompiledDefinition(decompiledSql);

        cleaned.Should().Contain("a character varying DEFAULT NULL");
        cleaned.Should().Contain("b integer DEFAULT NULL");
        cleaned.Should().Contain("c text DEFAULT ''");
        cleaned.Should().NotContain("NULL::character varying");
        cleaned.Should().NotContain("NULL::integer");
        cleaned.Should().NotContain("''::text");
    }

    [Test]
    public async Task CsprojProjectGenerator_UsesSourceFilePathForPlacement()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pgpac_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var projectFile = Path.Combine(tempDir, "TestDb.csproj");
            var generator = new CsprojProjectGenerator(projectFile);

            var project = new PgProject
            {
                DatabaseName = "TestDb",
                PostgresVersion = "16",
                Schemas = new List<PgSchema>
                {
                    new()
                    {
                        Name = "public",
                        Functions = new List<PgFunction>
                        {
                            new()
                            {
                                Name = "custom_func",
                                Definition = "CREATE FUNCTION public.custom_func() RETURNS void AS $$ BEGIN END; $$ LANGUAGE plpgsql;",
                                SourceFilePath = "CustomFolder/MyCustomFunc.sql"
                            }
                        }
                    }
                }
            };

            await generator.GenerateProjectAsync(project);

            var customFilePath = Path.Combine(tempDir, "CustomFolder", "MyCustomFunc.sql");
            File.Exists(customFilePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(customFilePath);
            content.Should().Contain("CREATE FUNCTION public.custom_func");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
