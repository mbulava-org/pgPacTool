using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// Tests for DependencyGraph enhancements (Phase 1, Task 1.1)
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Milestone2")]
public class DependencyGraphTests
{
    #region GetDependencies Tests

    [Test]
    public void GetDependencies_WithSimpleDependency_ReturnsList()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddDependency("public.orders", "public.users");
        
        // Act
        var deps = graph.GetDependencies("public.orders");
        
        // Assert
        Assert.That(deps, Has.Count.EqualTo(1));
        Assert.That(deps[0], Is.EqualTo("public.users"));
    }

    [Test]
    public void GetDependencies_WithMultipleDependencies_ReturnsAllDependencies()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.products", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddDependency("public.orders", "public.users");
        graph.AddDependency("public.orders", "public.products");
        
        // Act
        var deps = graph.GetDependencies("public.orders");
        
        // Assert
        Assert.That(deps, Has.Count.EqualTo(2));
        Assert.That(deps, Does.Contain("public.users"));
        Assert.That(deps, Does.Contain("public.products"));
    }

    [Test]
    public void GetDependencies_WithNoDependencies_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        
        // Act
        var deps = graph.GetDependencies("public.users");
        
        // Assert
        Assert.That(deps, Is.Empty);
    }

    [Test]
    public void GetDependencies_WithNonExistentObject_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        
        // Act
        var deps = graph.GetDependencies("public.nonexistent");
        
        // Assert
        Assert.That(deps, Is.Empty);
    }

    #endregion

    #region GetDependents Tests

    [Test]
    public void GetDependents_WithSimpleDependency_ReturnsList()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddDependency("public.orders", "public.users");
        
        // Act
        var dependents = graph.GetDependents("public.users");
        
        // Assert
        Assert.That(dependents, Has.Count.EqualTo(1));
        Assert.That(dependents[0], Is.EqualTo("public.orders"));
    }

    [Test]
    public void GetDependents_WithMultipleDependents_ReturnsAllDependents()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddObject("public.reviews", "TABLE");
        graph.AddDependency("public.orders", "public.users");
        graph.AddDependency("public.reviews", "public.users");
        
        // Act
        var dependents = graph.GetDependents("public.users");
        
        // Assert
        Assert.That(dependents, Has.Count.EqualTo(2));
        Assert.That(dependents, Does.Contain("public.orders"));
        Assert.That(dependents, Does.Contain("public.reviews"));
    }

    [Test]
    public void GetDependents_WithNoDependents_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.orders", "TABLE");
        
        // Act
        var dependents = graph.GetDependents("public.orders");
        
        // Assert
        Assert.That(dependents, Is.Empty);
    }

    #endregion

    #region HasPath Tests

    [Test]
    public void HasPath_WithDirectDependency_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddDependency("public.orders", "public.users");
        
        // Act
        var hasPath = graph.HasPath("public.orders", "public.users");
        
        // Assert
        Assert.That(hasPath, Is.True);
    }

    [Test]
    public void HasPath_WithIndirectDependency_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddObject("public.order_items", "TABLE");
        graph.AddDependency("public.order_items", "public.orders");
        graph.AddDependency("public.orders", "public.users");
        
        // Act
        var hasPath = graph.HasPath("public.order_items", "public.users");
        
        // Assert
        Assert.That(hasPath, Is.True);
    }

    [Test]
    public void HasPath_WithNoDependency_ReturnsFalse()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.products", "TABLE");
        
        // Act
        var hasPath = graph.HasPath("public.users", "public.products");
        
        // Assert
        Assert.That(hasPath, Is.False);
    }

    [Test]
    public void HasPath_ToSelf_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        
        // Act
        var hasPath = graph.HasPath("public.users", "public.users");
        
        // Assert
        Assert.That(hasPath, Is.True);
    }

    #endregion

    #region GetAllPaths Tests

    [Test]
    public void GetAllPaths_WithSinglePath_ReturnsOnePath()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddDependency("public.orders", "public.users");
        
        // Act
        var paths = graph.GetAllPaths("public.orders", "public.users");
        
        // Assert
        Assert.That(paths, Has.Count.EqualTo(1));
        Assert.That(paths[0], Has.Count.EqualTo(2));
        Assert.That(paths[0][0], Is.EqualTo("public.orders"));
        Assert.That(paths[0][1], Is.EqualTo("public.users"));
    }

    [Test]
    public void GetAllPaths_WithMultiplePaths_ReturnsAllPaths()
    {
        // Arrange - Diamond dependency
        //     users
        //    /     \
        // orders  reviews
        //    \     /
        //   order_reviews
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddObject("public.reviews", "TABLE");
        graph.AddObject("public.order_reviews", "TABLE");
        
        graph.AddDependency("public.orders", "public.users");
        graph.AddDependency("public.reviews", "public.users");
        graph.AddDependency("public.order_reviews", "public.orders");
        graph.AddDependency("public.order_reviews", "public.reviews");
        
        // Act
        var paths = graph.GetAllPaths("public.order_reviews", "public.users");
        
        // Assert
        Assert.That(paths, Has.Count.EqualTo(2));
        // Should have two paths:
        // 1. order_reviews -> orders -> users
        // 2. order_reviews -> reviews -> users
    }

    [Test]
    public void GetAllPaths_WithNoPath_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.products", "TABLE");
        
        // Act
        var paths = graph.GetAllPaths("public.users", "public.products");
        
        // Assert
        Assert.That(paths, Is.Empty);
    }

    #endregion

    #region GetObjectType Tests

    [Test]
    public void GetObjectType_ForExistingObject_ReturnsType()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.user_view", "VIEW");
        
        // Act
        var tableType = graph.GetObjectType("public.users");
        var viewType = graph.GetObjectType("public.user_view");
        
        // Assert
        Assert.That(tableType, Is.EqualTo("TABLE"));
        Assert.That(viewType, Is.EqualTo("VIEW"));
    }

    [Test]
    public void GetObjectType_ForNonExistentObject_ReturnsNull()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        
        // Act
        var type = graph.GetObjectType("public.nonexistent");
        
        // Assert
        Assert.That(type, Is.Null);
    }

    #endregion

    #region GetAllObjects Tests

    [Test]
    public void GetAllObjects_ReturnsAllAddedObjects()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddObject("public.products", "TABLE");
        
        // Act
        var objects = graph.GetAllObjects();
        
        // Assert
        Assert.That(objects, Has.Count.EqualTo(3));
        Assert.That(objects, Does.Contain("public.users"));
        Assert.That(objects, Does.Contain("public.orders"));
        Assert.That(objects, Does.Contain("public.products"));
    }

    [Test]
    public void GetAllObjects_OnEmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();
        
        // Act
        var objects = graph.GetAllObjects();
        
        // Assert
        Assert.That(objects, Is.Empty);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void ComplexGraph_AllMethodsWork()
    {
        // Arrange - Build a complex dependency graph
        var graph = new DependencyGraph();
        
        // Tables: users -> orders -> order_items -> products
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddObject("public.order_items", "TABLE");
        graph.AddObject("public.products", "TABLE");
        
        graph.AddDependency("public.orders", "public.users");
        graph.AddDependency("public.order_items", "public.orders");
        graph.AddDependency("public.order_items", "public.products");
        
        // Act & Assert
        // 1. Check dependencies
        var orderDeps = graph.GetDependencies("public.orders");
        Assert.That(orderDeps, Has.Count.EqualTo(1));
        Assert.That(orderDeps[0], Is.EqualTo("public.users"));
        
        // 2. Check dependents
        var userDependents = graph.GetDependents("public.users");
        Assert.That(userDependents, Has.Count.EqualTo(1));
        Assert.That(userDependents[0], Is.EqualTo("public.orders"));
        
        // 3. Check paths
        Assert.That(graph.HasPath("public.order_items", "public.users"), Is.True);
        Assert.That(graph.HasPath("public.users", "public.order_items"), Is.False);
        
        // 4. Check all objects
        var allObjects = graph.GetAllObjects();
        Assert.That(allObjects, Has.Count.EqualTo(4));
        
        // 5. Check object types
        Assert.That(graph.GetObjectType("public.users"), Is.EqualTo("TABLE"));
    }

    #endregion
}
