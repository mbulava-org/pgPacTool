using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// Tests for TopologicalSorter (Phase 3, Task 3.1)
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Milestone2")]
public class TopologicalSorterTests
{
    #region Basic Sorting

    [Test]
    public void Sort_WithLinearDependencies_ReturnsCorrectOrder()
    {
        // Arrange - A → B → C → D
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddObject("public.d", "TABLE");
        
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.c", "public.b");
        graph.AddDependency("public.d", "public.c");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Has.Count.EqualTo(4));
        
        // Dependencies should come before dependents
        int indexA = sorted.IndexOf("public.a");
        int indexB = sorted.IndexOf("public.b");
        int indexC = sorted.IndexOf("public.c");
        int indexD = sorted.IndexOf("public.d");
        
        Assert.That(indexA, Is.LessThan(indexB), "A should come before B");
        Assert.That(indexB, Is.LessThan(indexC), "B should come before C");
        Assert.That(indexC, Is.LessThan(indexD), "C should come before D");
    }

    [Test]
    public void Sort_WithNoDependencies_ReturnsAllObjects()
    {
        // Arrange - Independent objects
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Has.Count.EqualTo(3));
        Assert.That(sorted, Does.Contain("public.a"));
        Assert.That(sorted, Does.Contain("public.b"));
        Assert.That(sorted, Does.Contain("public.c"));
    }

    [Test]
    public void Sort_WithDiamondDependency_ReturnsValidOrder()
    {
        // Arrange - Diamond:
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddObject("public.d", "TABLE");
        
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.c", "public.a");
        graph.AddDependency("public.d", "public.b");
        graph.AddDependency("public.d", "public.c");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Has.Count.EqualTo(4));
        
        int indexA = sorted.IndexOf("public.a");
        int indexB = sorted.IndexOf("public.b");
        int indexC = sorted.IndexOf("public.c");
        int indexD = sorted.IndexOf("public.d");
        
        // A must come before B and C
        Assert.That(indexA, Is.LessThan(indexB));
        Assert.That(indexA, Is.LessThan(indexC));
        
        // B and C must both come before D
        Assert.That(indexB, Is.LessThan(indexD));
        Assert.That(indexC, Is.LessThan(indexD));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Sort_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Is.Empty);
    }

    [Test]
    public void Sort_SingleObject_ReturnsSingleObject()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Has.Count.EqualTo(1));
        Assert.That(sorted[0], Is.EqualTo("public.users"));
    }

    [Test]
    public void Sort_WithCircularDependency_ThrowsException()
    {
        // Arrange - A → B → A
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        
        var sorter = new TopologicalSorter();
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sorter.Sort(graph));
    }

    [Test]
    public void Sort_WithDependencyOutsideGraph_IgnoresMissingDependency()
    {
        // Arrange - users depends on an external object not represented in the graph
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");

        graph.AddDependency("public.users", "public.users_id_seq");

        var sorter = new TopologicalSorter();

        // Act
        var sorted = sorter.Sort(graph);

        // Assert
        Assert.That(sorted, Has.Count.EqualTo(1));
        Assert.That(sorted[0], Is.EqualTo("public.users"));
    }

    #endregion

    #region Complex Graphs

    [Test]
    public void Sort_ComplexGraph_ReturnsValidOrder()
    {
        // Arrange - Complex dependency graph
        var graph = new DependencyGraph();
        
        // users -> orders -> order_items -> products
        //                  \             /
        //                   categories --
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.orders", "TABLE");
        graph.AddObject("public.order_items", "TABLE");
        graph.AddObject("public.products", "TABLE");
        graph.AddObject("public.categories", "TABLE");
        
        graph.AddDependency("public.orders", "public.users");
        graph.AddDependency("public.order_items", "public.orders");
        graph.AddDependency("public.order_items", "public.products");
        graph.AddDependency("public.products", "public.categories");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Has.Count.EqualTo(5));
        
        // Verify key orderings
        int usersIdx = sorted.IndexOf("public.users");
        int ordersIdx = sorted.IndexOf("public.orders");
        int orderItemsIdx = sorted.IndexOf("public.order_items");
        int productsIdx = sorted.IndexOf("public.products");
        int categoriesIdx = sorted.IndexOf("public.categories");
        
        Assert.That(usersIdx, Is.LessThan(ordersIdx));
        Assert.That(ordersIdx, Is.LessThan(orderItemsIdx));
        Assert.That(productsIdx, Is.LessThan(orderItemsIdx));
        Assert.That(categoriesIdx, Is.LessThan(productsIdx));
    }

    [Test]
    public void Sort_DisconnectedComponents_ReturnsAllObjects()
    {
        // Arrange - Two separate graphs: A → B and C → D
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddObject("public.d", "TABLE");
        
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.d", "public.c");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var sorted = sorter.Sort(graph);
        
        // Assert
        Assert.That(sorted, Has.Count.EqualTo(4));
        
        // Each component should maintain order
        int indexA = sorted.IndexOf("public.a");
        int indexB = sorted.IndexOf("public.b");
        int indexC = sorted.IndexOf("public.c");
        int indexD = sorted.IndexOf("public.d");
        
        Assert.That(indexA, Is.LessThan(indexB));
        Assert.That(indexC, Is.LessThan(indexD));
    }

    #endregion

    #region SortInLevels Tests

    [Test]
    public void SortInLevels_WithLinearDependencies_ReturnsLevels()
    {
        // Arrange - A → B → C
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.c", "public.b");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var levels = sorter.SortInLevels(graph);
        
        // Assert
        Assert.That(levels, Has.Count.EqualTo(3));
        Assert.That(levels[0], Does.Contain("public.a"));
        Assert.That(levels[1], Does.Contain("public.b"));
        Assert.That(levels[2], Does.Contain("public.c"));
    }

    [Test]
    public void SortInLevels_WithParallelObjects_GroupsInSameLevel()
    {
        // Arrange - Independent B and C both depend on A
        //     A
        //    / \
        //   B   C
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.c", "public.a");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var levels = sorter.SortInLevels(graph);
        
        // Assert
        Assert.That(levels, Has.Count.EqualTo(2));
        Assert.That(levels[0], Does.Contain("public.a"));
        
        // B and C should be in same level (can be created in parallel)
        Assert.That(levels[1], Does.Contain("public.b"));
        Assert.That(levels[1], Does.Contain("public.c"));
    }

    [Test]
    public void SortInLevels_ComplexGraph_ReturnsCorrectLevels()
    {
        // Arrange - Diamond graph
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddObject("public.d", "TABLE");
        
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.c", "public.a");
        graph.AddDependency("public.d", "public.b");
        graph.AddDependency("public.d", "public.c");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var levels = sorter.SortInLevels(graph);
        
        // Assert
        Assert.That(levels, Has.Count.EqualTo(3));
        Assert.That(levels[0], Does.Contain("public.a"));
        Assert.That(levels[1], Does.Contain("public.b"));
        Assert.That(levels[1], Does.Contain("public.c"));
        Assert.That(levels[2], Does.Contain("public.d"));
    }

    #endregion

    #region CanSort Tests

    [Test]
    public void CanSort_WithValidDAG_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.b", "public.a");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var canSort = sorter.CanSort(graph);
        
        // Assert
        Assert.That(canSort, Is.True);
    }

    [Test]
    public void CanSort_WithCircularDependency_ReturnsFalse()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        
        var sorter = new TopologicalSorter();
        
        // Act
        var canSort = sorter.CanSort(graph);
        
        // Assert
        Assert.That(canSort, Is.False);
    }

    [Test]
    public void CanSort_EmptyGraph_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        var sorter = new TopologicalSorter();
        
        // Act
        var canSort = sorter.CanSort(graph);
        
        // Assert
        Assert.That(canSort, Is.True);
    }

    #endregion
}
