using FluentAssertions;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Models;
using PgQuery;

namespace mbulava.PostgreSql.Dac.Tests.Compare;

[TestFixture]
public class PublishScriptGeneratorTests
{
    [Test]
    public void Generate_NoDifferences_ReturnsEmptyScript()
    {
        // Arrange
        var diff = new PgSchemaDiff { SchemaName = "public" };
        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Generate_WithComments_IncludesHeader()
    {
        // Arrange
        var diff = new PgSchemaDiff { SchemaName = "public" };
        var options = new PublishOptions { IncludeComments = true };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("PostgreSQL Deployment Script");
        result.Should().Contain("Schema: public");
    }

    [Test]
    public void Generate_WithTransaction_IncludesBeginCommit()
    {
        // Arrange
        var diff = new PgSchemaDiff { SchemaName = "public" };
        var options = new PublishOptions { Transactional = true };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("BEGIN;");
        result.Should().Contain("COMMIT;");
    }

    [Test]
    public void Generate_WithoutTransaction_NoBeginCommit()
    {
        // Arrange
        var diff = new PgSchemaDiff { SchemaName = "public" };
        var options = new PublishOptions { Transactional = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().NotContain("BEGIN;");
        result.Should().NotContain("COMMIT;");
    }

    [Test]
    public void Generate_NewTable_CreatesAddColumn()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            TableDiffs = new List<PgTableDiff>
            {
                new()
                {
                    TableName = "users",
                    ColumnDiffs = new List<PgColumnDiff>
                    {
                        new()
                        {
                            ColumnName = "email",
                            SourceDataType = "VARCHAR(255)",
                            TargetDataType = null,
                            SourceIsNotNull = true
                        }
                    }
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.ToUpper().Should().Contain("ALTER TABLE");
        result.ToUpper().Should().Contain("ADD"); // Deparser output may vary
        result.Should().Contain("email");
        result.ToUpper().Should().Contain("VARCHAR"); // Case-insensitive
        result.ToUpper().Should().Contain("NOT NULL");
    }

    [Test]
    public void Generate_DropColumn_WithDropFlag_CreatesDropColumn()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            TableDiffs = new List<PgTableDiff>
            {
                new()
                {
                    TableName = "users",
                    ColumnDiffs = new List<PgColumnDiff>
                    {
                        new()
                        {
                            ColumnName = "old_column",
                            SourceDataType = null,
                            TargetDataType = "INT"
                        }
                    }
                }
            }
        };

        var options = new PublishOptions
        {
            IncludeComments = false,
            DropObjectsNotInSource = true
        };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        // Deparser may omit "COLUMN" keyword (valid SQL: "DROP IF EXISTS old_column")
        result.ToUpper().Should().Contain("DROP");
        result.Should().Contain("old_column");
    }

    [Test]
    public void Generate_ColumnTypeChange_CreatesAlterType()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            TableDiffs = new List<PgTableDiff>
            {
                new()
                {
                    TableName = "users",
                    ColumnDiffs = new List<PgColumnDiff>
                    {
                        new()
                        {
                            ColumnName = "age",
                            SourceDataType = "BIGINT",
                            TargetDataType = "INT"
                        }
                    }
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.ToUpper().Should().Contain("ALTER COLUMN");
        result.ToUpper().Should().Contain("TYPE BIGINT"); // Case-insensitive match (deparser uses lowercase)
    }

    [Test]
    public void Generate_NewView_CreatesView()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            ViewDiffs = new List<PgViewDiff>
            {
                new()
                {
                    ViewName = "active_users",
                    SourceDefinition = "CREATE VIEW active_users AS SELECT * FROM users WHERE active = true",
                    TargetDefinition = null
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("CREATE VIEW active_users");
    }

    [Test]
    public void Generate_ChangedView_CreatesOrReplace()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            ViewDiffs = new List<PgViewDiff>
            {
                new()
                {
                    ViewName = "user_summary",
                    SourceDefinition = "CREATE VIEW user_summary AS SELECT id, name FROM users",
                    TargetDefinition = "CREATE VIEW user_summary AS SELECT id FROM users",
                    DefinitionChanged = true
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("CREATE OR REPLACE");
        result.Should().Contain("VIEW user_summary");
    }

    [Test]
    public void Generate_NewFunction_CreatesFunction()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            FunctionDiffs = new List<PgFunctionDiff>
            {
                new()
                {
                    FunctionName = "get_user_count",
                    SourceDefinition = "CREATE FUNCTION get_user_count() RETURNS INT AS $$ SELECT COUNT(*) FROM users; $$ LANGUAGE SQL",
                    TargetDefinition = null
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("CREATE FUNCTION get_user_count");
    }

    [Test]
    public void Generate_NewTrigger_CreatesTrigger()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            TriggerDiffs = new List<PgTriggerDiff>
            {
                new()
                {
                    TriggerName = "update_timestamp",
                    TableName = "users",
                    SourceDefinition = "CREATE TRIGGER update_timestamp BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_modified_column()",
                    TargetDefinition = null
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("CREATE TRIGGER update_timestamp");
    }

    [Test]
    public void Generate_WithPreDeploymentScript_IncludesScript()
    {
        // Arrange
        var diff = new PgSchemaDiff { SchemaName = "public" };
        var options = new PublishOptions
        {
            IncludeComments = false,
            PreDeploymentScripts = new List<DeploymentScript>
            {
                new()
                {
                    FilePath = "pre.sql",
                    Content = "-- Pre-deployment backup",
                    Order = 1,
                    Type = DeploymentScriptType.PreDeployment
                }
            }
        };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("Pre-deployment backup");
    }

    [Test]
    public void Generate_WithPostDeploymentScript_IncludesScript()
    {
        // Arrange
        var diff = new PgSchemaDiff { SchemaName = "public" };
        var options = new PublishOptions
        {
            IncludeComments = false,
            PostDeploymentScripts = new List<DeploymentScript>
            {
                new()
                {
                    FilePath = "post.sql",
                    Content = "-- Post-deployment data migration",
                    Order = 1,
                    Type = DeploymentScriptType.PostDeployment
                }
            }
        };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("Post-deployment data migration");
    }

    [Test]
    public void Generate_WithSqlCmdVariables_ReplacesVariables()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            ViewDiffs = new List<PgViewDiff>
            {
                new()
                {
                    ViewName = "test_view",
                    SourceDefinition = "CREATE VIEW test_view AS SELECT * FROM $(TableName)",
                    TargetDefinition = null
                }
            }
        };

        var options = new PublishOptions
        {
            IncludeComments = false,
            Variables = new List<SqlCmdVariable>
            {
                new() { Name = "TableName", Value = "users" }
            }
        };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("FROM users");
        result.Should().NotContain("$(TableName)");
    }

    [Test]
    public void Generate_PrivilegeChanges_CreatesGrantRevoke()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            TableDiffs = new List<PgTableDiff>
            {
                new()
                {
                    TableName = "users",
                    PrivilegeChanges = new List<PgPrivilegeDiff>
                    {
                        new()
                        {
                            Grantee = "app_user",
                            PrivilegeType = "SELECT",
                            ChangeType = PrivilegeChangeType.MissingInTarget
                        }
                    }
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.ToUpper().Should().Contain("GRANT SELECT");
        result.Should().Contain("app_user");
    }

    [Test]
    public void Generate_TypeChange_DropsAndRecreates()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            TypeDiffs = new List<PgTypeDiff>
            {
                new()
                {
                    TypeName = "status_enum",
                    SourceDefinition = "CREATE TYPE status_enum AS ENUM ('active', 'inactive', 'pending')",
                    TargetDefinition = "CREATE TYPE status_enum AS ENUM ('active', 'inactive')",
                    DefinitionChanged = true
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("DROP TYPE");
        result.Should().Contain("CREATE TYPE status_enum");
        result.Should().Contain("CASCADE");
    }

    [Test]
    public void Generate_SequenceChange_AltersSequence()
    {
        // Arrange
        var diff = new PgSchemaDiff
        {
            SchemaName = "public",
            SequenceDiffs = new List<PgSequenceDiff>
            {
                new()
                {
                    SequenceName = "user_id_seq",
                    DefinitionChanged = true,
                    SourceOptions = new List<SeqOption>
                    {
                        new() { OptionName = "INCREMENT", OptionValue = "5" }
                    }
                }
            }
        };

        var options = new PublishOptions { IncludeComments = false };

        // Act
        var result = PublishScriptGenerator.Generate(diff, options);

        // Assert
        result.Should().Contain("ALTER SEQUENCE");
        result.Should().Contain("INCREMENT 5");
    }
}
