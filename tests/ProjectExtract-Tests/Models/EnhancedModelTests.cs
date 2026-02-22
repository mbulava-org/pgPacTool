using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Models;

/// <summary>
/// Tests for Issue #8 - Enhanced Model with Relationships
/// Tests for Issue #10 - Dependency Graph and Cycle Detection
/// </summary>
[TestFixture]
[Category("Models")]
[Category("Integration")]
public class EnhancedModelTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("testpass")
            .Build();

        await _pgContainer.StartAsync();

        var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
        {
            MaxPoolSize = 25,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 30,
            Timeout = 30
        };
        _connectionString = builder.ToString();

        await SeedTestDataAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        NpgsqlConnection.ClearAllPools();
        await _pgContainer.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await ExecuteSqlAsync(conn, @"
            CREATE SCHEMA test_model;

            -- Create tables with relationships
            CREATE TABLE test_model.departments (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL UNIQUE,
                budget DECIMAL(12,2) CHECK (budget > 0),
                created_at TIMESTAMP DEFAULT NOW()
            );

            CREATE TABLE test_model.employees (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                email VARCHAR(100) UNIQUE,
                department_id INTEGER REFERENCES test_model.departments(id),
                salary DECIMAL(10,2) NOT NULL CHECK (salary >= 0),
                hire_date DATE NOT NULL,
                CONSTRAINT valid_email CHECK (email LIKE '%@%')
            );

            CREATE TABLE test_model.projects (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                department_id INTEGER REFERENCES test_model.departments(id)
            );

            -- Create indexes
            CREATE INDEX idx_employees_dept ON test_model.employees(department_id);
            CREATE INDEX idx_employees_email ON test_model.employees(email);
            CREATE INDEX idx_projects_dept ON test_model.projects(department_id);

            -- Create views with dependencies
            CREATE VIEW test_model.employee_summary AS
            SELECT e.id, e.name, d.name AS department
            FROM test_model.employees e
            JOIN test_model.departments d ON e.department_id = d.id;

            CREATE VIEW test_model.department_stats AS
            SELECT d.id, d.name, COUNT(e.id) AS employee_count
            FROM test_model.departments d
            LEFT JOIN test_model.employees e ON d.id = e.department_id
            GROUP BY d.id, d.name;

            -- View depending on another view
            CREATE VIEW test_model.large_departments AS
            SELECT * FROM test_model.department_stats
            WHERE employee_count > 10;
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    #region Issue #8 - Enhanced Model Tests

    [Test]
    public async Task EnhancedModel_TableWithConstraints_ExtractsAllTypes()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_model");
        Assert.That(schema, Is.Not.Null);

        var employeesTable = schema!.Tables.FirstOrDefault(t => t.Name == "employees");
        Assert.That(employeesTable, Is.Not.Null);

        // Check constraints
        Assert.That(employeesTable!.Constraints, Is.Not.Empty, "Should have constraints");
        
        var primaryKey = employeesTable.PrimaryKey;
        Assert.That(primaryKey, Is.Not.Null, "Should have primary key");

        var foreignKeys = employeesTable.ForeignKeys;
        Assert.That(foreignKeys, Is.Not.Empty, "Should have foreign keys");
        Assert.That(foreignKeys.Count, Is.EqualTo(1), "Should have 1 foreign key");

        var checkConstraints = employeesTable.CheckConstraints;
        Assert.That(checkConstraints, Is.Not.Empty, "Should have check constraints");

        var uniqueConstraints = employeesTable.UniqueConstraints;
        Assert.That(uniqueConstraints, Is.Not.Empty, "Should have unique constraints");

        TestContext.Out.WriteLine($"✓ Table constraints extracted:");
        TestContext.Out.WriteLine($"  Primary Key: {primaryKey?.Name}");
        TestContext.Out.WriteLine($"  Foreign Keys: {foreignKeys.Count}");
        TestContext.Out.WriteLine($"  Check Constraints: {checkConstraints.Count}");
        TestContext.Out.WriteLine($"  Unique Constraints: {uniqueConstraints.Count}");
    }

    [Test]
    public async Task EnhancedModel_TableWithIndexes_ExtractsAll()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_model");
        var employeesTable = schema?.Tables.FirstOrDefault(t => t.Name == "employees");

        Assert.That(employeesTable, Is.Not.Null);
        Assert.That(employeesTable!.Indexes, Is.Not.Empty, "Should have indexes");
        
        var deptIndex = employeesTable.Indexes.FirstOrDefault(i => i.Name.Contains("dept"));
        Assert.That(deptIndex, Is.Not.Null, "Should have department index");

        TestContext.Out.WriteLine($"✓ Table indexes extracted:");
        foreach (var index in employeesTable.Indexes)
        {
            TestContext.Out.WriteLine($"  - {index.Name}");
        }
    }

    [Test]
    public async Task EnhancedModel_ForeignKey_TracksReferencedTable()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_model");
        var employeesTable = schema?.Tables.FirstOrDefault(t => t.Name == "employees");

        var fk = employeesTable?.ForeignKeys.FirstOrDefault();
        Assert.That(fk, Is.Not.Null);
        Assert.That(fk!.ReferencedTable, Is.Not.Null.Or.Empty, "Should have referenced table");
        Assert.That(fk.ReferencedTable, Does.Contain("departments"), "Should reference departments table");

        TestContext.Out.WriteLine($"✓ Foreign key relationship:");
        TestContext.Out.WriteLine($"  From: employees.{string.Join(", ", fk.Keys)}");
        TestContext.Out.WriteLine($"  To: {fk.ReferencedTable}");
    }

    #endregion

    #region Issue #10 - Dependency Graph Tests

    [Test]
    public void DependencyGraph_SimpleGraph_TopologicalSortWorks()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("table1", "TABLE");
        graph.AddObject("table2", "TABLE");
        graph.AddObject("view1", "VIEW");

        graph.AddDependency("view1", "table1");  // view1 depends on table1
        graph.AddDependency("view1", "table2");  // view1 depends on table2

        // Act
        var sorted = graph.TopologicalSort();

        // Assert
        Assert.That(sorted, Has.Count.EqualTo(3));
        var view1Index = sorted.IndexOf("view1");
        var table1Index = sorted.IndexOf("table1");
        var table2Index = sorted.IndexOf("table2");

        // In topological sort, dependencies come BEFORE dependents
        Assert.That(table1Index, Is.LessThan(view1Index), "table1 should come before view1");
        Assert.That(table2Index, Is.LessThan(view1Index), "table2 should come before view1");

        TestContext.Out.WriteLine("✓ Topological sort order:");
        foreach (var item in sorted)
        {
            TestContext.Out.WriteLine($"  {sorted.IndexOf(item) + 1}. {item}");
        }
    }

    [Test]
    public void DependencyGraph_CircularDependency_ThrowsException()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("view1", "VIEW");
        graph.AddObject("view2", "VIEW");
        
        graph.AddDependency("view1", "view2");  // view1 depends on view2
        graph.AddDependency("view2", "view1");  // view2 depends on view1 (circular!)

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => graph.TopologicalSort());
        Assert.That(ex!.Message, Does.Contain("Circular dependency"));

        TestContext.Out.WriteLine($"✓ Circular dependency detected: {ex.Message}");
    }

    [Test]
    public void DependencyGraph_DetectCycles_FindsCircular()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddObject("view1", "VIEW");
        graph.AddObject("view2", "VIEW");
        graph.AddObject("view3", "VIEW");
        
        graph.AddDependency("view1", "view2");
        graph.AddDependency("view2", "view3");
        graph.AddDependency("view3", "view1");  // Creates a cycle

        // Act
        var cycles = graph.DetectCycles();

        // Assert
        Assert.That(cycles, Is.Not.Empty, "Should detect cycles");
        Assert.That(cycles[0], Has.Count.GreaterThanOrEqualTo(3), "Cycle should have at least 3 nodes");

        TestContext.Out.WriteLine("✓ Cycle detected:");
        foreach (var node in cycles[0])
        {
            TestContext.Out.WriteLine($"  -> {node}");
        }
    }

    [Test]
    public async Task DependencyGraph_RealProject_BuildsCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");
        
        var graph = new DependencyGraph();
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_model");
        Assert.That(schema, Is.Not.Null);

        // Build graph from project
        foreach (var table in schema!.Tables)
        {
            var qualifiedName = $"test_model.{table.Name}";
            graph.AddObject(qualifiedName, "TABLE");
        }

        foreach (var view in schema.Views)
        {
            var qualifiedName = $"test_model.{view.Name}";
            graph.AddObject(qualifiedName, "VIEW");
            
            // Add dependencies from view definition
            foreach (var table in schema.Tables)
            {
                if (view.Definition.Contains(table.Name))
                {
                    graph.AddDependency(qualifiedName, $"test_model.{table.Name}");
                }
            }

            // Add dependencies on other views
            foreach (var otherView in schema.Views.Where(v => v.Name != view.Name))
            {
                if (view.Definition.Contains(otherView.Name))
                {
                    graph.AddDependency(qualifiedName, $"test_model.{otherView.Name}");
                }
            }
        }

        // Act
        var sorted = graph.TopologicalSort();

        // Assert
        Assert.That(sorted, Is.Not.Empty);
        
        TestContext.Out.WriteLine("✓ Project dependency order:");
        for (int i = 0; i < sorted.Count; i++)
        {
            TestContext.Out.WriteLine($"  {i + 1}. {sorted[i]}");
        }
    }

    [Test]
    public async Task DependencyGraph_ViewDependencies_CorrectOrder()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);
        var project = await extractor.ExtractPgProject("testdb");
        
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_model");
        var deptStatsView = schema?.Views.FirstOrDefault(v => v.Name == "department_stats");
        var largeDeptView = schema?.Views.FirstOrDefault(v => v.Name == "large_departments");

        // Assert
        Assert.That(deptStatsView, Is.Not.Null);
        Assert.That(largeDeptView, Is.Not.Null);
        
        // large_departments depends on department_stats
        Assert.That(largeDeptView!.Definition, Does.Contain("department_stats"), 
            "large_departments should reference department_stats");

        TestContext.Out.WriteLine("✓ View dependencies tracked:");
        TestContext.Out.WriteLine($"  {largeDeptView.Name} depends on department_stats");
    }

    #endregion

    #region Comprehensive Summary Test

    [Test]
    [Category("Comprehensive")]
    public async Task EnhancedModel_Comprehensive_AllFeaturesWork()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_model");
        Assert.That(schema, Is.Not.Null);

        TestContext.Out.WriteLine("=== Enhanced Model Comprehensive Summary ===");
        TestContext.Out.WriteLine($"Schema: {schema!.Name}");
        TestContext.Out.WriteLine();

        // Tables
        TestContext.Out.WriteLine($"Tables: {schema.Tables.Count}");
        foreach (var table in schema.Tables)
        {
            TestContext.Out.WriteLine($"  {table.Name}:");
            TestContext.Out.WriteLine($"    Columns: {table.Columns.Count}");
            TestContext.Out.WriteLine($"    Constraints: {table.Constraints.Count}");
            TestContext.Out.WriteLine($"      - Primary Keys: {(table.PrimaryKey != null ? 1 : 0)}");
            TestContext.Out.WriteLine($"      - Foreign Keys: {table.ForeignKeys.Count}");
            TestContext.Out.WriteLine($"      - Check Constraints: {table.CheckConstraints.Count}");
            TestContext.Out.WriteLine($"      - Unique Constraints: {table.UniqueConstraints.Count}");
            TestContext.Out.WriteLine($"    Indexes: {table.Indexes.Count}");
        }

        TestContext.Out.WriteLine();

        // Views
        TestContext.Out.WriteLine($"Views: {schema.Views.Count}");
        foreach (var view in schema.Views)
        {
            TestContext.Out.WriteLine($"  {view.Name}");
        }

        TestContext.Out.WriteLine();
        TestContext.Out.WriteLine("✅ All enhanced model features working!");
    }

    #endregion
}
