using FluentAssertions;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Models;

/// <summary>
/// Tests for PgDependency model
/// </summary>
[TestFixture]
[Category("Models")]
public class PgDependencyTests
{
    [Test]
    public void PgDependency_DefaultValues_AreEmpty()
    {
        // Arrange & Act
        var dependency = new PgDependency();

        // Assert
        dependency.ObjectType.Should().BeEmpty();
        dependency.ObjectSchema.Should().BeEmpty();
        dependency.ObjectName.Should().BeEmpty();
        dependency.DependsOnType.Should().BeEmpty();
        dependency.DependsOnSchema.Should().BeEmpty();
        dependency.DependsOnName.Should().BeEmpty();
        dependency.DependencyType.Should().BeEmpty();
    }

    [Test]
    public void PgDependency_Properties_CanBeSet()
    {
        // Arrange
        var dependency = new PgDependency();

        // Act
        dependency.ObjectType = "VIEW";
        dependency.ObjectSchema = "public";
        dependency.ObjectName = "user_view";
        dependency.DependsOnType = "TABLE";
        dependency.DependsOnSchema = "public";
        dependency.DependsOnName = "users";
        dependency.DependencyType = "NORMAL";

        // Assert
        dependency.ObjectType.Should().Be("VIEW");
        dependency.ObjectSchema.Should().Be("public");
        dependency.ObjectName.Should().Be("user_view");
        dependency.DependsOnType.Should().Be("TABLE");
        dependency.DependsOnSchema.Should().Be("public");
        dependency.DependsOnName.Should().Be("users");
        dependency.DependencyType.Should().Be("NORMAL");
    }

    [Test]
    public void PgDependency_QualifiedObjectName_CombinesSchemaAndName()
    {
        // Arrange
        var dependency = new PgDependency
        {
            ObjectSchema = "public",
            ObjectName = "user_view"
        };

        // Act
        var qualifiedName = dependency.QualifiedObjectName;

        // Assert
        qualifiedName.Should().Be("public.user_view");
    }

    [Test]
    public void PgDependency_QualifiedDependsOnName_CombinesSchemaAndName()
    {
        // Arrange
        var dependency = new PgDependency
        {
            DependsOnSchema = "public",
            DependsOnName = "users"
        };

        // Act
        var qualifiedName = dependency.QualifiedDependsOnName;

        // Assert
        qualifiedName.Should().Be("public.users");
    }

    [Test]
    public void PgDependency_ViewDependsOnTable_Scenario()
    {
        // Arrange & Act
        var dependency = new PgDependency
        {
            ObjectType = "VIEW",
            ObjectSchema = "public",
            ObjectName = "active_users",
            DependsOnType = "TABLE",
            DependsOnSchema = "public",
            DependsOnName = "users",
            DependencyType = "NORMAL"
        };

        // Assert
        dependency.QualifiedObjectName.Should().Be("public.active_users");
        dependency.QualifiedDependsOnName.Should().Be("public.users");
        dependency.ObjectType.Should().Be("VIEW");
        dependency.DependsOnType.Should().Be("TABLE");
    }

    [Test]
    public void PgDependency_FunctionDependsOnTable_Scenario()
    {
        // Arrange & Act
        var dependency = new PgDependency
        {
            ObjectType = "FUNCTION",
            ObjectSchema = "public",
            ObjectName = "get_user_count",
            DependsOnType = "TABLE",
            DependsOnSchema = "public",
            DependsOnName = "users",
            DependencyType = "NORMAL"
        };

        // Assert
        dependency.QualifiedObjectName.Should().Be("public.get_user_count");
        dependency.QualifiedDependsOnName.Should().Be("public.users");
    }

    [Test]
    public void PgDependency_CrossSchemaDependency_Scenario()
    {
        // Arrange & Act
        var dependency = new PgDependency
        {
            ObjectType = "VIEW",
            ObjectSchema = "reporting",
            ObjectName = "user_report",
            DependsOnType = "VIEW",
            DependsOnSchema = "public",
            DependsOnName = "users_view",
            DependencyType = "NORMAL"
        };

        // Assert
        dependency.QualifiedObjectName.Should().Be("reporting.user_report");
        dependency.QualifiedDependsOnName.Should().Be("public.users_view");
    }

    [Test]
    public void PgDependency_AutoDependency_Scenario()
    {
        // Arrange & Act
        var dependency = new PgDependency
        {
            ObjectType = "TABLE",
            ObjectSchema = "public",
            ObjectName = "orders",
            DependsOnType = "SEQUENCE",
            DependsOnSchema = "public",
            DependsOnName = "orders_id_seq",
            DependencyType = "AUTO"
        };

        // Assert
        dependency.DependencyType.Should().Be("AUTO");
        dependency.QualifiedObjectName.Should().Be("public.orders");
        dependency.QualifiedDependsOnName.Should().Be("public.orders_id_seq");
    }
}
