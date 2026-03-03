using FluentAssertions;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compare;

/// <summary>
/// Tests for SeqOptionComparer equality comparer
/// </summary>
[TestFixture]
[Category("Comparers")]
public class SeqOptionComparerTests
{
    private SeqOptionComparer _comparer = null!;

    [SetUp]
    public void SetUp()
    {
        _comparer = new SeqOptionComparer();
    }

    [Test]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        SeqOption? x = null;
        SeqOption? y = null;

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_OneNull_ReturnsFalse()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        SeqOption? y = null;

        // Act
        var result1 = _comparer.Equals(x, y);
        var result2 = _comparer.Equals(y, x);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Test]
    public void Equals_SameOptionNameAndValue_ReturnsTrue()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "START", OptionValue = "1" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_SameOptionNameAndValue_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "start", OptionValue = "1" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentOptionName_ReturnsFalse()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "INCREMENT", OptionValue = "1" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentOptionValue_ReturnsFalse()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "START", OptionValue = "10" };

        // Act
        var result = _comparer.Equals(x, y);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void GetHashCode_SameOptions_ReturnsSameHash()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "START", OptionValue = "1" };

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
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "start", OptionValue = "1" };

        // Act
        var hash1 = _comparer.GetHashCode(x);
        var hash2 = _comparer.GetHashCode(y);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Test]
    public void GetHashCode_DifferentOptions_ReturnsDifferentHash()
    {
        // Arrange
        var x = new SeqOption { OptionName = "START", OptionValue = "1" };
        var y = new SeqOption { OptionName = "INCREMENT", OptionValue = "1" };

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
        var options = new HashSet<SeqOption>(_comparer)
        {
            new SeqOption { OptionName = "START", OptionValue = "1" },
            new SeqOption { OptionName = "INCREMENT", OptionValue = "1" },
            new SeqOption { OptionName = "MINVALUE", OptionValue = "1" }
        };

        // Act - Try to add duplicate (different case)
        var duplicate = new SeqOption { OptionName = "start", OptionValue = "1" };
        var added = options.Add(duplicate);

        // Assert
        added.Should().BeFalse("duplicate should not be added");
        options.Should().HaveCount(3);
    }

    [Test]
    public void CommonSequenceOptions_AreEqual()
    {
        // Arrange & Act
        var options = new[]
        {
            (new SeqOption { OptionName = "START", OptionValue = "1" },
             new SeqOption { OptionName = "start", OptionValue = "1" }),
            
            (new SeqOption { OptionName = "INCREMENT", OptionValue = "1" },
             new SeqOption { OptionName = "increment", OptionValue = "1" }),
            
            (new SeqOption { OptionName = "MAXVALUE", OptionValue = "9223372036854775807" },
             new SeqOption { OptionName = "maxvalue", OptionValue = "9223372036854775807" }),
        };

        // Assert
        foreach (var (x, y) in options)
        {
            _comparer.Equals(x, y).Should().BeTrue();
        }
    }
}
