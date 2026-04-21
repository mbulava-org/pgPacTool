using FluentAssertions;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

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
}
