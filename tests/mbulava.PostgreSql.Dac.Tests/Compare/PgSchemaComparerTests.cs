using FluentAssertions;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;
using PgQuery;

namespace mbulava.PostgreSql.Dac.Tests.Compare;

/// <summary>
/// Tests for PgSchemaComparer
/// </summary>
[TestFixture]
[Category("Comparers")]
public class PgSchemaComparerTests
{
    private PgSchemaComparer _comparer = null!;
    private CompareOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _comparer = new PgSchemaComparer();
        _options = new CompareOptions();
    }

    [Test]
    public void Compare_IdenticalSchemas_ReturnsNoDifferences()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres"
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres"
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.Should().NotBeNull();
        diff.SchemaName.Should().Be("public");
        diff.OwnerChanged.Should().BeNull();
        diff.PrivilegeChanges.Should().BeEmpty();
    }

    [Test]
    public void Compare_DifferentOwners_DetectsOwnerChange()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres"
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "newowner"
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.OwnerChanged.Should().NotBeNull();
        diff.OwnerChanged.Value.SourceOwner.Should().Be("postgres");
        diff.OwnerChanged.Value.TargetOwner.Should().Be("newowner");
    }

    [Test]
    public void Compare_EmptySourceOwner_DoesNotDetectOwnerChange()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = string.Empty
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres"
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.OwnerChanged.Should().BeNull();
    }

    [Test]
    public void Compare_MissingPrivilege_DetectsDifference()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Privileges = new()
            {
                new PgPrivilege
                {
                    Grantee = "user1",
                    PrivilegeType = "USAGE",
                    IsGrantable = false
                }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Privileges = new()
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.PrivilegeChanges.Should().HaveCount(1);
        diff.PrivilegeChanges[0].ChangeType.Should().Be(PrivilegeChangeType.MissingInTarget);
        diff.PrivilegeChanges[0].Grantee.Should().Be("user1");
    }

    [Test]
    public void Compare_ExtraPrivilege_DetectsDifference()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Privileges = new()
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Privileges = new()
            {
                new PgPrivilege
                {
                    Grantee = "user1",
                    PrivilegeType = "USAGE",
                    IsGrantable = false
                }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.PrivilegeChanges.Should().HaveCount(1);
        diff.PrivilegeChanges[0].ChangeType.Should().Be(PrivilegeChangeType.ExtraInTarget);
        diff.PrivilegeChanges[0].Grantee.Should().Be("user1");
    }

    [Test]
    public void Compare_MissingTable_DetectedInTableDiffs()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable { Name = "users" }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().NotBeEmpty();
        diff.TableDiffs[0].TableName.Should().Be("users");
    }

    [Test]
    public void Compare_MissingSequence_DetectedInSequenceDiffs()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Sequences = new()
            {
                new PgSequence { Name = "user_id_seq" }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Sequences = new()
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.SequenceDiffs.Should().NotBeEmpty();
        diff.SequenceDiffs[0].SequenceName.Should().Be("user_id_seq");
    }

    [Test]
    public void Compare_MissingView_DetectedInViewDiffs()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Views = new()
            {
                new PgView { Name = "active_users" }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Views = new()
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.ViewDiffs.Should().NotBeEmpty();
    }

    [Test]
    public void Compare_MissingFunction_DetectedInFunctionDiffs()
    {
        // Arrange
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Functions = new()
            {
                new PgFunction { Name = "get_user_count" }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Functions = new()
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.FunctionDiffs.Should().NotBeEmpty();
    }

    // ── Column-level diffs ────────────────────────────────────────────────────

    [Test]
    public void Compare_DroppedColumn_DetectedAsDiff()
    {
        // Arrange: source has "email" column, target does not (dropped)
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "users",
                    Columns = new()
                    {
                        new PgColumn { Name = "id",    DataType = "integer" },
                        new PgColumn { Name = "email", DataType = "text" }
                    }
                }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "users",
                    Columns = new()
                    {
                        new PgColumn { Name = "id", DataType = "integer" }
                    }
                }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().HaveCount(1);
        var tableDiff = diff.TableDiffs[0];
        tableDiff.TableName.Should().Be("users");
        tableDiff.ColumnDiffs.Should().HaveCount(1);
        var colDiff = tableDiff.ColumnDiffs[0];
        colDiff.ColumnName.Should().Be("email");
        colDiff.SourceDataType.Should().Be("text");
        colDiff.TargetDataType.Should().BeNull("column is absent in target");
    }

    [Test]
    public void Compare_RenamedColumn_DetectedAsDiff()
    {
        // Rename is modelled as: source column missing in target + extra column in target.
        // Arrange: source has "username", target has "user_name" (renamed)
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "users",
                    Columns = new()
                    {
                        new PgColumn { Name = "id",       DataType = "integer" },
                        new PgColumn { Name = "username", DataType = "text" }
                    }
                }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "users",
                    Columns = new()
                    {
                        new PgColumn { Name = "id",        DataType = "integer" },
                        new PgColumn { Name = "user_name", DataType = "text" }
                    }
                }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().HaveCount(1);
        var tableDiff = diff.TableDiffs[0];
        tableDiff.TableName.Should().Be("users");
        // "username" missing in target + "user_name" extra in target
        tableDiff.ColumnDiffs.Should().HaveCount(2);
        tableDiff.ColumnDiffs.Should().Contain(c => c.ColumnName == "username"  && c.TargetDataType == null);
        tableDiff.ColumnDiffs.Should().Contain(c => c.ColumnName == "user_name" && c.SourceDataType == null);
    }

    // ── Constraint diffs ──────────────────────────────────────────────────────

    [Test]
    public void Compare_ChangedConstraint_DetectedAsDiff()
    {
        // Arrange: same constraint name, different definition (check expression changed)
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "orders",
                    Constraints = new()
                    {
                        new PgConstraint
                        {
                            Name = "chk_amount",
                            Type = ConstrType.ConstrCheck,
                            Definition = "CHECK (amount > 0)"
                        }
                    }
                }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "orders",
                    Constraints = new()
                    {
                        new PgConstraint
                        {
                            Name = "chk_amount",
                            Type = ConstrType.ConstrCheck,
                            Definition = "CHECK (amount >= 0)"  // changed
                        }
                    }
                }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().HaveCount(1);
        var tableDiff = diff.TableDiffs[0];
        tableDiff.TableName.Should().Be("orders");
        tableDiff.ConstraintDiffs.Should().HaveCount(1);
        var cDiff = tableDiff.ConstraintDiffs[0];
        cDiff.ConstraintName.Should().Be("chk_amount");
        cDiff.SourceDefinition.Should().Be("CHECK (amount > 0)");
        cDiff.TargetDefinition.Should().Be("CHECK (amount >= 0)");
    }

    [Test]
    public void Compare_MissingConstraint_DetectedAsDiff()
    {
        // Arrange: source has a unique constraint absent in target
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "users",
                    Constraints = new()
                    {
                        new PgConstraint
                        {
                            Name = "uq_email",
                            Type = ConstrType.ConstrUnique,
                            Definition = "UNIQUE (email)"
                        }
                    }
                }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable { Name = "users", Constraints = new() }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().HaveCount(1);
        var tableDiff = diff.TableDiffs[0];
        tableDiff.ConstraintDiffs.Should().HaveCount(1);
        tableDiff.ConstraintDiffs[0].ConstraintName.Should().Be("uq_email");
        tableDiff.ConstraintDiffs[0].TargetDefinition.Should().BeNull();
    }

    // ── Index diffs ───────────────────────────────────────────────────────────

    [Test]
    public void Compare_AddedIndex_DetectedAsDiff()
    {
        // Arrange: target has a new index absent in source
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable { Name = "users", Indexes = new() }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "users",
                    Indexes = new()
                    {
                        new PgIndex
                        {
                            Name = "idx_users_email",
                            Definition = "CREATE INDEX idx_users_email ON users (email)"
                        }
                    }
                }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().HaveCount(1);
        var tableDiff = diff.TableDiffs[0];
        tableDiff.TableName.Should().Be("users");
        tableDiff.IndexDiffs.Should().HaveCount(1);
        var idxDiff = tableDiff.IndexDiffs[0];
        idxDiff.IndexName.Should().Be("idx_users_email");
        idxDiff.SourceDefinition.Should().BeNull("index is new in target");
        idxDiff.TargetDefinition.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void Compare_ChangedIndexDefinition_DetectedAsDiff()
    {
        // Arrange: same index name, different definition (different columns)
        var source = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "events",
                    Indexes = new()
                    {
                        new PgIndex
                        {
                            Name = "idx_events_ts",
                            Definition = "CREATE INDEX idx_events_ts ON events (created_at)"
                        }
                    }
                }
            }
        };

        var target = new PgSchema
        {
            Name = "public",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable
                {
                    Name = "events",
                    Indexes = new()
                    {
                        new PgIndex
                        {
                            Name = "idx_events_ts",
                            Definition = "CREATE INDEX idx_events_ts ON events (created_at DESC)"
                        }
                    }
                }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.TableDiffs.Should().HaveCount(1);
        diff.TableDiffs[0].IndexDiffs.Should().HaveCount(1);
        diff.TableDiffs[0].IndexDiffs[0].IndexName.Should().Be("idx_events_ts");
        diff.TableDiffs[0].IndexDiffs[0].SourceDefinition.Should().Contain("created_at)");
        diff.TableDiffs[0].IndexDiffs[0].TargetDefinition.Should().Contain("created_at DESC");
    }

    // ── Multi-schema compare ──────────────────────────────────────────────────

    [Test]
    public void MultiSchema_Compare_DetectsDiff_InNonPublicSchema()
    {
        // Arrange: compare two schemas named "billing" — one has an extra table
        var source = new PgSchema
        {
            Name = "billing",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable { Name = "invoices" },
                new PgTable { Name = "payments" }
            }
        };

        var target = new PgSchema
        {
            Name = "billing",
            Owner = "postgres",
            Tables = new()
            {
                new PgTable { Name = "invoices" }
                // "payments" missing
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.SchemaName.Should().Be("billing");
        diff.TableDiffs.Should().HaveCount(1);
        diff.TableDiffs[0].TableName.Should().Be("payments");
    }

    [Test]
    public void MultiSchema_Compare_IdenticalNonPublicSchemas_ReturnsNoDiff()
    {
        // Arrange: two identical "analytics" schemas
        var source = new PgSchema
        {
            Name = "analytics",
            Owner = "analyst",
            Tables = new()
            {
                new PgTable { Name = "events" }
            }
        };

        var target = new PgSchema
        {
            Name = "analytics",
            Owner = "analyst",
            Tables = new()
            {
                new PgTable { Name = "events" }
            }
        };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.SchemaName.Should().Be("analytics");
        diff.TableDiffs.Should().BeEmpty();
        diff.OwnerChanged.Should().BeNull();
    }

    [Test]
    public void MultiSchema_Compare_OwnerChanged_InCustomSchema()
    {
        // Arrange: "reporting" schema with different owners
        var source = new PgSchema { Name = "reporting", Owner = "postgres" };
        var target = new PgSchema { Name = "reporting", Owner = "report_admin" };

        // Act
        var diff = _comparer.Compare(source, target, _options);

        // Assert
        diff.SchemaName.Should().Be("reporting");
        diff.OwnerChanged.Should().NotBeNull();
        diff.OwnerChanged!.Value.SourceOwner.Should().Be("postgres");
        diff.OwnerChanged!.Value.TargetOwner.Should().Be("report_admin");
    }
}
