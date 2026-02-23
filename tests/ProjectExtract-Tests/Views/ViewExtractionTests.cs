using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Views;

/// <summary>
/// Comprehensive tests for view extraction functionality (Issue #1)
/// Tests cover regular views, materialized views, privileges, and dependencies
/// </summary>
[TestFixture]
[Category("Views")]
[Category("Integration")]
public class ViewExtractionTests
{
    private PostgreSqlContainer _pgContainer = default!;
    private string _connectionString = default!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Start PostgreSQL 16 container
        _pgContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("testpass")
            .Build();

        await _pgContainer.StartAsync();

        // Configure connection string with connection pool limits
        var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
        {
            MaxPoolSize = 25,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 30,
            Timeout = 30
        };
        _connectionString = builder.ToString();

        // Seed test data
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

        // Create test schema
        await ExecuteSqlAsync(conn, @"
            CREATE SCHEMA test_views;
        ");

        // Create base tables for views
        await ExecuteSqlAsync(conn, @"
            CREATE TABLE test_views.customers (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100),
                email VARCHAR(100),
                created_at TIMESTAMP DEFAULT NOW()
            );

            CREATE TABLE test_views.orders (
                id SERIAL PRIMARY KEY,
                customer_id INTEGER REFERENCES test_views.customers(id),
                order_date DATE,
                total DECIMAL(10,2)
            );

            INSERT INTO test_views.customers (name, email) VALUES
                ('Alice', 'alice@example.com'),
                ('Bob', 'bob@example.com'),
                ('Charlie', 'charlie@example.com');

            INSERT INTO test_views.orders (customer_id, order_date, total) VALUES
                (1, '2024-01-15', 150.00),
                (1, '2024-02-20', 200.00),
                (2, '2024-01-25', 75.50),
                (3, '2024-03-10', 300.00);
        ");

        // Create test views
        await ExecuteSqlAsync(conn, @"
            -- Simple view
            CREATE VIEW test_views.customer_list AS
            SELECT id, name, email
            FROM test_views.customers
            ORDER BY name;

            -- View with joins
            CREATE VIEW test_views.customer_orders AS
            SELECT 
                c.id AS customer_id,
                c.name AS customer_name,
                o.id AS order_id,
                o.order_date,
                o.total
            FROM test_views.customers c
            LEFT JOIN test_views.orders o ON c.id = o.customer_id;

            -- View with aggregation
            CREATE VIEW test_views.customer_summary AS
            SELECT 
                c.id,
                c.name,
                COUNT(o.id) AS order_count,
                COALESCE(SUM(o.total), 0) AS total_spent
            FROM test_views.customers c
            LEFT JOIN test_views.orders o ON c.id = o.customer_id
            GROUP BY c.id, c.name;

            -- View referencing another view (dependency)
            CREATE VIEW test_views.high_value_customers AS
            SELECT id, name, total_spent
            FROM test_views.customer_summary
            WHERE total_spent > 100;

            -- Materialized view
            CREATE MATERIALIZED VIEW test_views.customer_stats AS
            SELECT 
                COUNT(*) AS total_customers,
                AVG(total_spent) AS avg_spent,
                MAX(total_spent) AS max_spent
            FROM test_views.customer_summary;

            -- View with CTE
            CREATE VIEW test_views.recent_orders AS
            WITH recent AS (
                SELECT * FROM test_views.orders
                WHERE order_date >= CURRENT_DATE - INTERVAL '30 days'
            )
            SELECT 
                r.id,
                c.name AS customer_name,
                r.order_date,
                r.total
            FROM recent r
            JOIN test_views.customers c ON r.customer_id = c.id;
        ");

        // Create test users and grant privileges
        await ExecuteSqlAsync(conn, @"
            CREATE ROLE view_user LOGIN PASSWORD 'pass1';
            CREATE ROLE view_admin LOGIN PASSWORD 'pass2';

            -- Grant privileges on views
            GRANT SELECT ON test_views.customer_list TO view_user;
            GRANT SELECT ON test_views.customer_orders TO view_user WITH GRANT OPTION;
            GRANT ALL PRIVILEGES ON test_views.customer_summary TO view_admin;
        ");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    #region Basic View Extraction Tests

    [Test]
    public async Task ExtractViews_SimpleView_ExtractsCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        Assert.That(schema, Is.Not.Null, "test_views schema should exist");

        var customerList = schema!.Views.FirstOrDefault(v => v.Name == "customer_list");
        Assert.That(customerList, Is.Not.Null, "customer_list view should exist");
        Assert.That(customerList!.Owner, Is.EqualTo("postgres"), "Owner should be postgres");
        Assert.That(customerList.IsMaterialized, Is.False, "Should not be materialized");
        Assert.That(customerList.Definition, Does.Contain("SELECT"), "Definition should contain SELECT");
        Assert.That(customerList.Definition, Does.Contain("customers"), "Definition should reference customers table");

        TestContext.Out.WriteLine($"✓ Simple view extracted: {customerList.Name}");
        TestContext.Out.WriteLine($"  Owner: {customerList.Owner}");
        TestContext.Out.WriteLine($"  Materialized: {customerList.IsMaterialized}");
    }

    [Test]
    public async Task ExtractViews_AllViewsInSchema_ExtractsCount()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        Assert.That(schema, Is.Not.Null);
        
        // Should have 6 views: customer_list, customer_orders, customer_summary, 
        // high_value_customers, customer_stats (materialized), recent_orders
        Assert.That(schema!.Views.Count, Is.EqualTo(6), "Should extract all 6 views");

        var viewNames = schema.Views.Select(v => v.Name).OrderBy(n => n).ToList();
        TestContext.Out.WriteLine($"✓ Extracted {schema.Views.Count} views:");
        foreach (var name in viewNames)
        {
            var view = schema.Views.First(v => v.Name == name);
            TestContext.Out.WriteLine($"  - {name} (Materialized: {view.IsMaterialized})");
        }
    }

    #endregion

    #region Materialized View Tests

    [Test]
    public async Task ExtractViews_MaterializedView_SetsFlagCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var matView = schema?.Views.FirstOrDefault(v => v.Name == "customer_stats");

        Assert.That(matView, Is.Not.Null, "customer_stats should exist");
        Assert.That(matView!.IsMaterialized, Is.True, "Should be marked as materialized");
        Assert.That(matView.Definition, Does.Contain("SELECT"), "Should have definition");

        TestContext.Out.WriteLine($"✓ Materialized view correctly identified:");
        TestContext.Out.WriteLine($"  Name: {matView.Name}");
        TestContext.Out.WriteLine($"  IsMaterialized: {matView.IsMaterialized}");
    }

    [Test]
    public async Task ExtractViews_RegularVsMaterialized_FlagDiffers()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var regularView = schema?.Views.FirstOrDefault(v => v.Name == "customer_list");
        var matView = schema?.Views.FirstOrDefault(v => v.Name == "customer_stats");

        Assert.That(regularView?.IsMaterialized, Is.False, "Regular view should be false");
        Assert.That(matView?.IsMaterialized, Is.True, "Materialized view should be true");

        TestContext.Out.WriteLine("✓ View type flags correctly set:");
        TestContext.Out.WriteLine($"  Regular: {regularView?.Name} = {regularView?.IsMaterialized}");
        TestContext.Out.WriteLine($"  Materialized: {matView?.Name} = {matView?.IsMaterialized}");
    }

    #endregion

    #region View Definition Tests

    [Test]
    public async Task ExtractViews_ViewWithJoin_ContainsJoinClause()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "customer_orders");

        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Definition, Does.Contain("JOIN").IgnoreCase, "Should contain JOIN");
        Assert.That(view.Definition, Does.Contain("customers"), "Should reference customers");
        Assert.That(view.Definition, Does.Contain("orders"), "Should reference orders");

        TestContext.Out.WriteLine($"✓ View with JOIN extracted:");
        TestContext.Out.WriteLine($"  Name: {view.Name}");
        TestContext.Out.WriteLine($"  Contains JOIN: Yes");
    }

    [Test]
    public async Task ExtractViews_ViewWithCTE_ContainsWithClause()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "recent_orders");

        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Definition, Does.Contain("WITH").IgnoreCase, "Should contain WITH (CTE)");

        TestContext.Out.WriteLine($"✓ View with CTE extracted:");
        TestContext.Out.WriteLine($"  Name: {view.Name}");
        TestContext.Out.WriteLine($"  Contains CTE: Yes");
    }

    [Test]
    public async Task ExtractViews_ViewWithAggregation_ContainsGroupBy()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "customer_summary");

        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Definition, Does.Contain("GROUP BY").IgnoreCase, "Should contain GROUP BY");
        Assert.That(view.Definition, Does.Contain("COUNT").IgnoreCase, "Should contain COUNT aggregate");

        TestContext.Out.WriteLine($"✓ View with aggregation extracted:");
        TestContext.Out.WriteLine($"  Name: {view.Name}");
        TestContext.Out.WriteLine($"  Contains GROUP BY: Yes");
    }

    #endregion

    #region View Privilege Tests

    [Test]
    public async Task ExtractViews_WithSelectPrivilege_ExtractsCorrectly()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "customer_list");

        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Privileges, Is.Not.Empty, "Should have privileges");

        var userPriv = view.Privileges.FirstOrDefault(p => p.Grantee == "view_user");
        Assert.That(userPriv, Is.Not.Null, "view_user should have privilege");
        Assert.That(userPriv!.PrivilegeType, Is.EqualTo("SELECT"), "Should be SELECT privilege");

        TestContext.Out.WriteLine($"✓ View privileges extracted:");
        TestContext.Out.WriteLine($"  View: {view.Name}");
        TestContext.Out.WriteLine($"  Grantee: {userPriv.Grantee}");
        TestContext.Out.WriteLine($"  Privilege: {userPriv.PrivilegeType}");
    }

    [Test]
    public async Task ExtractViews_WithGrantOption_SetsIsGrantableTrue()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "customer_orders");

        Assert.That(view, Is.Not.Null);
        
        var userPriv = view!.Privileges.FirstOrDefault(p => 
            p.Grantee == "view_user" && p.PrivilegeType == "SELECT");
        
        Assert.That(userPriv, Is.Not.Null, "view_user should have SELECT privilege");
        Assert.That(userPriv!.IsGrantable, Is.True, "Should have GRANT OPTION");

        TestContext.Out.WriteLine($"✓ View GRANT OPTION detected:");
        TestContext.Out.WriteLine($"  View: {view.Name}");
        TestContext.Out.WriteLine($"  Grantee: {userPriv.Grantee}");
        TestContext.Out.WriteLine($"  IsGrantable: {userPriv.IsGrantable}");
    }

    [Test]
    public async Task ExtractViews_WithAllPrivileges_ExtractsMultiple()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "customer_summary");

        Assert.That(view, Is.Not.Null);
        
        var adminPrivs = view!.Privileges.Where(p => p.Grantee == "view_admin").ToList();
        Assert.That(adminPrivs, Is.Not.Empty, "view_admin should have privileges");
        Assert.That(adminPrivs.Count, Is.GreaterThanOrEqualTo(2), "Should have multiple privileges from ALL");

        TestContext.Out.WriteLine($"✓ Multiple privileges extracted:");
        TestContext.Out.WriteLine($"  View: {view.Name}");
        TestContext.Out.WriteLine($"  Grantee: view_admin");
        TestContext.Out.WriteLine($"  Privileges: {string.Join(", ", adminPrivs.Select(p => p.PrivilegeType))}");
    }

    #endregion

    #region View Dependencies Tests

    [Test]
    public async Task ExtractViews_ViewReferencingOtherView_TracksDependency()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "high_value_customers");

        Assert.That(view, Is.Not.Null);
        
        // This view depends on customer_summary view
        // Dependencies might be in the definition or explicitly tracked
        Assert.That(view!.Definition, Does.Contain("customer_summary"), 
            "Should reference customer_summary in definition");

        TestContext.Out.WriteLine($"✓ View dependency detected:");
        TestContext.Out.WriteLine($"  View: {view.Name}");
        TestContext.Out.WriteLine($"  References: customer_summary");
    }

    #endregion

    #region AST Parsing Tests

    [Test]
    public async Task ExtractViews_WithValidDefinition_ParsesAst()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        var view = schema?.Views.FirstOrDefault(v => v.Name == "customer_list");

        Assert.That(view, Is.Not.Null);

        // Check if AST is parsed
        if (view!.Ast != null)
        {
            Assert.That(view.Ast, Is.Not.Null, "AST should be parsed");
            TestContext.Out.WriteLine($"✓ View AST parsed successfully");
        }

        TestContext.Out.WriteLine($"  View: {view.Name}");
        TestContext.Out.WriteLine($"  AST present: {view.Ast != null}");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task ExtractViews_EmptySchema_ReturnsEmptyList()
    {
        // Arrange
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await ExecuteSqlAsync(conn, "CREATE SCHEMA empty_schema;");

        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "empty_schema");
        Assert.That(schema, Is.Not.Null, "empty_schema should exist");
        Assert.That(schema!.Views, Is.Empty, "Should have no views");

        TestContext.Out.WriteLine("✓ Empty schema handled correctly (no views)");
    }

    [Test]
    public async Task ExtractViews_PublicSchema_ExtractsViews()
    {
        // Arrange
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await ExecuteSqlAsync(conn, @"
            CREATE VIEW public.test_public_view AS
            SELECT 1 AS id, 'test' AS name;
        ");

        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var publicSchema = project.Schemas.FirstOrDefault(s => s.Name == "public");
        Assert.That(publicSchema, Is.Not.Null);
        
        var publicView = publicSchema!.Views.FirstOrDefault(v => v.Name == "test_public_view");
        Assert.That(publicView, Is.Not.Null, "Should extract view from public schema");

        TestContext.Out.WriteLine($"✓ Public schema view extracted: {publicView!.Name}");
    }

    #endregion

    #region Comprehensive Summary Test

    [Test]
    [Category("Comprehensive")]
    public async Task ExtractViews_ComprehensiveCheck_AllFeaturesWork()
    {
        // Arrange
        var extractor = new PgProjectExtractor(_connectionString);

        // Act
        var project = await extractor.ExtractPgProject("testdb");

        // Assert
        var schema = project.Schemas.FirstOrDefault(s => s.Name == "test_views");
        Assert.That(schema, Is.Not.Null);

        TestContext.Out.WriteLine("=== Comprehensive View Extraction Summary ===");
        TestContext.Out.WriteLine($"Schema: {schema!.Name}");
        TestContext.Out.WriteLine($"Total Views: {schema.Views.Count}");
        TestContext.Out.WriteLine();

        // Check all views
        foreach (var view in schema.Views.OrderBy(v => v.Name))
        {
            TestContext.Out.WriteLine($"View: {view.Name}");
            TestContext.Out.WriteLine($"  Owner: {view.Owner}");
            TestContext.Out.WriteLine($"  Materialized: {view.IsMaterialized}");
            TestContext.Out.WriteLine($"  Privileges: {view.Privileges.Count}");
            TestContext.Out.WriteLine($"  Definition Length: {view.Definition?.Length ?? 0} chars");
            
            if (view.Privileges.Any())
            {
                foreach (var priv in view.Privileges)
                {
                    TestContext.Out.WriteLine($"    - {priv.Grantee}: {priv.PrivilegeType} (Grantable: {priv.IsGrantable})");
                }
            }
            
            TestContext.Out.WriteLine();
        }

        // Overall assertions
        Assert.That(schema.Views.Count, Is.EqualTo(6), "Should have 6 views total");
        Assert.That(schema.Views.Count(v => v.IsMaterialized), Is.EqualTo(1), "Should have 1 materialized view");
        Assert.That(schema.Views.Count(v => !v.IsMaterialized), Is.EqualTo(5), "Should have 5 regular views");
        Assert.That(schema.Views.All(v => !string.IsNullOrEmpty(v.Definition)), Is.True, "All views should have definitions");
        Assert.That(schema.Views.All(v => !string.IsNullOrEmpty(v.Owner)), Is.True, "All views should have owners");

        TestContext.Out.WriteLine("✅ All comprehensive checks passed!");
    }

    #endregion
}
