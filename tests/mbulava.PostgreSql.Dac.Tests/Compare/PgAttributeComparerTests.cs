using FluentAssertions;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compare;

/// <summary>
/// Tests for PgAttributeComparer equality comparer
/// </summary>
[TestFixture]
[Category("Comparers")]
public class PgAttributeComparerTests
{
    private PgAttributeComparer _comparer = null!;

    [SetUp]
    public void SetUp()
    {
        _comparer = new PgAttributeComparer();
    }

    [Test]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        PgAttribute? x = null;
        PgAttribute? y = null;

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_OneNull_ReturnsFalse()
    {
        // Arrange
        var x = new PgAttribute { Name = "id", DataType = "integer" };
        PgAttribute? y = null;

        // Act
        var result1 = _comparer.Equals(x, y);
        var result2 = _comparer.Equals(y, x);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Test]
    public void Equals_SameNameAndType_ReturnsTrue()
    {
        // Arrange
        var x = new PgAttribute { Name = "id", DataType = "integer" };
        var y = new PgAttribute { Name = "id", DataType = "integer" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_SameNameAndType_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var x = new PgAttribute { Name = "ID", DataType = "INTEGER" };
        var y = new PgAttribute { Name = "id", DataType = "integer" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentName_ReturnsFalse()
    {
        // Arrange
        var x = new PgAttribute { Name = "id", DataType = "integer" };
        var y = new PgAttribute { Name = "name", DataType = "integer" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var x = new PgAttribute { Name = "id", DataType = "integer" };
        var y = new PgAttribute { Name = "id", DataType = "bigint" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void GetHashCode_SameAttributes_ReturnsSameHash()
    {
        // Arrange
        var x = new PgAttribute { Name = "id", DataType = "integer" };
        var y = new PgAttribute { Name = "id", DataType = "integer" };

        // Act
        var hash1 = _comparer.GetHashCode(x);
        var hash2 = _comparer.GetHashCode(y);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void GetHashCode_CaseInsensitive_ReturnsSameHash()
    {
        // Arrange
        var x = new PgAttribute { Name = "ID", DataType = "INTEGER" };
        var y = new PgAttribute { Name = "id", DataType = "integer" };

        // Act
        var hash1 = _comparer.GetHashCode(x);
        var hash2 = _comparer.GetHashCode(y);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void GetHashCode_DifferentAttributes_ReturnsDifferentHash()
    {
        // Arrange
        var x = new PgAttribute { Name = "id", DataType = "integer" };
        var y = new PgAttribute { Name = "name", DataType = "text" };

        // Act
        var hash1 = _comparer.GetHashCode(x);
        var hash2 = _comparer.GetHashCode(y);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var attrs = new HashSet<PgAttribute>(_comparer)
        {
            new PgAttribute { Name = "id", DataType = "integer" },
            new PgAttribute { Name = "name", DataType = "text" }
        };

        // Act - Try to add duplicate (different case)
        var duplicate = new PgAttribute { Name = "ID", DataType = "INTEGER" };
        var added = attrs.Add(duplicate);

        // Assert
        added.Should().BeFalse("duplicate should not be added");
        attrs.Should().HaveCount(2);
    }
}
