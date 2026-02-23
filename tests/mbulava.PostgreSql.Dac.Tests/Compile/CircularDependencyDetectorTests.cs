using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// Tests for CircularDependencyDetector (Phase 2, Task 2.1)
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Milestone2")]
public class CircularDependencyDetectorTests
{
    #region Simple Cycle Detection

    [Test]
    public void DetectCycles_WithSimpleCycle_ReturnsCycle()
    {
        // Arrange - A → B → A
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Has.Count.GreaterThanOrEqualTo(1));
        var cycle = cycles[0];
        Assert.That(cycle.Cycle, Does.Contain("public.a"));
        Assert.That(cycle.Cycle, Does.Contain("public.b"));
    }

    [Test]
    public void DetectCycles_WithThreeNodeCycle_ReturnsCycle()
    {
        // Arrange - A → B → C → A
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.c");
        graph.AddDependency("public.c", "public.a");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Has.Count.GreaterThanOrEqualTo(1));
        var cycle = cycles[0];
        Assert.That(cycle.Cycle.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void DetectCycles_WithDAG_ReturnsNoCycles()
    {
        // Arrange - A → B → C (no cycle)
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.c");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Is.Empty);
    }

    #endregion

    #region Complex Cycles

    [Test]
    public void DetectCycles_WithMultipleCycles_ReturnsAllCycles()
    {
        // Arrange - Two separate cycles: A → B → A and C → D → C
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddObject("public.d", "TABLE");
        
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        graph.AddDependency("public.c", "public.d");
        graph.AddDependency("public.d", "public.c");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void DetectCycles_WithDiamondDAG_ReturnsNoCycles()
    {
        // Arrange - Diamond (not a cycle):
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
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Is.Empty);
    }

    #endregion

    #region Special Cases

    [Test]
    public void DetectCycles_WithSelfReference_AllowsOrReports()
    {
        // Arrange - Self-referencing table (allowed in PostgreSQL for some cases)
        var graph = new DependencyGraph();
        graph.AddObject("public.employees", "TABLE");
        graph.AddDependency("public.employees", "public.employees");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        // Self-references might be allowed depending on type
        // For tables with self-referential FK, we should allow
        // For views, we should reject
        Assert.That(cycles, Is.Not.Null);
    }

    [Test]
    public void DetectCycles_RecursiveFunction_AllowsOrReports()
    {
        // Arrange - Recursive function (allowed in PostgreSQL)
        var graph = new DependencyGraph();
        graph.AddObject("public.factorial", "FUNCTION");
        graph.AddDependency("public.factorial", "public.factorial");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        // Recursive functions are allowed
        Assert.That(cycles, Is.Not.Null);
    }

    [Test]
    public void DetectCycles_CircularViews_ReturnsCycle()
    {
        // Arrange - View A → View B → View A (NOT allowed)
        var graph = new DependencyGraph();
        graph.AddObject("public.view_a", "VIEW");
        graph.AddObject("public.view_b", "VIEW");
        graph.AddDependency("public.view_a", "public.view_b");
        graph.AddDependency("public.view_b", "public.view_a");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Has.Count.GreaterThanOrEqualTo(1));
        var cycle = cycles[0];
        Assert.That(cycle.Severity, Is.EqualTo(CycleSeverity.Error));
    }

    #endregion

    #region HasCycles

    [Test]
    public void HasCycles_WithCycle_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var hasCycles = detector.HasCycles(graph);
        
        // Assert
        Assert.That(hasCycles, Is.True);
    }

    [Test]
    public void HasCycles_WithoutCycle_ReturnsFalse()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.a", "public.b");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var hasCycles = detector.HasCycles(graph);
        
        // Assert
        Assert.That(hasCycles, Is.False);
    }

    #endregion

    #region Cycle Analysis

    [Test]
    public void DetectCycles_IncludesObjectTypes()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        graph.AddObject("public.user_view", "VIEW");
        graph.AddDependency("public.users", "public.user_view");
        graph.AddDependency("public.user_view", "public.users");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Has.Count.GreaterThanOrEqualTo(1));
        var cycle = cycles[0];
        Assert.That(cycle.ObjectTypes, Is.Not.Null);
        Assert.That(cycle.ObjectTypes, Does.Contain("TABLE"));
        Assert.That(cycle.ObjectTypes, Does.Contain("VIEW"));
    }

    [Test]
    public void DetectCycles_ProvidesSuggestions()
    {
        // Arrange - View cycle
        var graph = new DependencyGraph();
        graph.AddObject("public.view_a", "VIEW");
        graph.AddObject("public.view_b", "VIEW");
        graph.AddDependency("public.view_a", "public.view_b");
        graph.AddDependency("public.view_b", "public.view_a");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Has.Count.GreaterThanOrEqualTo(1));
        var cycle = cycles[0];
        Assert.That(cycle.Suggestion, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region FindAllCycles

    [Test]
    public void FindAllCycles_WithSingleCycle_ReturnsOnePath()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cyclePaths = detector.FindAllCycles(graph);
        
        // Assert
        Assert.That(cyclePaths, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(cyclePaths[0], Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void FindAllCycles_WithMultipleCycles_ReturnsAllPaths()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.a", "TABLE");
        graph.AddObject("public.b", "TABLE");
        graph.AddObject("public.c", "TABLE");
        graph.AddObject("public.d", "TABLE");
        
        // Cycle 1: A → B → A
        graph.AddDependency("public.a", "public.b");
        graph.AddDependency("public.b", "public.a");
        
        // Cycle 2: C → D → C
        graph.AddDependency("public.c", "public.d");
        graph.AddDependency("public.d", "public.c");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cyclePaths = detector.FindAllCycles(graph);
        
        // Assert
        Assert.That(cyclePaths.Count, Is.GreaterThanOrEqualTo(2));
    }

    #endregion

    #region Empty/Edge Cases

    [Test]
    public void DetectCycles_EmptyGraph_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Is.Empty);
    }

    [Test]
    public void DetectCycles_SingleNodeNoDependency_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("public.users", "TABLE");
        
        var detector = new CircularDependencyDetector();
        
        // Act
        var cycles = detector.DetectCycles(graph);
        
        // Assert
        Assert.That(cycles, Is.Empty);
    }

    #endregion
}
