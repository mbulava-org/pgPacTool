# pgPacTool - GitHub Issues Tracker

**Project:** PostgreSQL Data-Tier Application Compiler  
**Target Framework:** .NET 10  
**Status:** Planning Phase  
**Last Updated:** 2026-01-31

---

## Table of Contents

- [High Priority - MVP Issues](#high-priority---mvp-issues)
  - [Issue #1: Implement View Extraction](#issue-1-implement-view-extraction-from-postgresql-database)
  - [Issue #2: Implement Function Extraction](#issue-2-implement-function-extraction-from-postgresql-database)
  - [Issue #3: Implement Stored Procedure Extraction](#issue-3-implement-stored-procedure-extraction-postgresql-11)
  - [Issue #4: Implement Trigger Extraction](#issue-4-implement-trigger-extraction-from-postgresql-database)
  - [Issue #5: Implement Index Extraction](#issue-5-implement-index-extraction-from-postgresql-database)
  - [Issue #6: Implement Constraint Extraction](#issue-6-implement-constraint-extraction-fk-check-unique-exclusion)
  - [Issue #7: Fix Privilege Extraction Bug](#issue-7-fix-privilege-extraction-bug)
  - [Issue #8: Enhance PgTable Model Properties](#issue-8-enhance-pgtable-model-properties)
  - [Issue #9: Implement Compiler Reference Validation](#issue-9-implement-compiler-reference-validation)
  - [Issue #10: Implement Circular Dependency Detection](#issue-10-implement-circular-dependency-detection)
  - [Issue #11: Create Integration Tests Infrastructure](#issue-11-create-integration-tests-with-testcontainers)
- [Medium Priority Issues](#medium-priority-issues)
- [Lower Priority Issues](#lower-priority-issues)
- [Issue Statistics](#issue-statistics)

---

## High Priority - MVP Issues

### Issue #1: Implement View Extraction from PostgreSQL Database

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Extraction  
**Story Points:** 5  
**Estimated Time:** 1-2 days  
**Target Version:** v0.1.0

#### Description

Currently, view extraction is commented out in `PgProjectExtractor.cs`. We need to implement complete view extraction including definitions, dependencies, and privileges to align with MSBuild.Sdk.SqlProj functionality.

**Current State:**
```csharp
// Line ~65 in PgProjectExtractor.cs
//pgSchema.Views.AddRange(await ExtractViewsAsync(schema.Name));
```

**Why This Matters:**
Views are essential database objects that need to be extracted and represented in the DAC model. Without this, users cannot extract complete database schemas.

#### Acceptance Criteria

- [ ] Implement `ExtractViewsAsync(string schemaName)` method in `PgProjectExtractor.cs`
- [ ] Extract view definition SQL using `pg_get_viewdef()`
- [ ] Extract view owner from `pg_roles`
- [ ] Extract view privileges/grants
- [ ] Parse view definition into AST using mbulava-org.Npgquery library
- [ ] Store parsed AST in `PgView` model class
- [ ] Handle materialized views separately (flag `IsMaterialized`)
- [ ] Create `PgView` model class in `Models/DbObjects.cs` with properties:
  - `Name` (string)
  - `Schema` (string)
  - `Owner` (string)
  - `Definition` (string - SQL text)
  - `Ast` (parsed representation)
  - `AstJson` (string - optional JSON representation)
  - `IsMaterialized` (bool)
  - `Privileges` (List<PgPrivilege>)
  - `Dependencies` (List<string> - referenced tables/views)
- [ ] Handle views with dependencies on other views (respect dependency order)
- [ ] Include views in `PgSchema.Views` collection
- [ ] Add views to JSON serialization output

#### Technical Implementation

**SQL Query to Extract Views:**
```sql
SELECT 
    c.relname AS view_name,
    r.rolname AS owner,
    pg_get_viewdef(c.oid, true) AS definition,
    c.relkind = 'm' AS is_materialized
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
JOIN pg_roles r ON r.oid = c.relowner
WHERE n.nspname = @schema
  AND c.relkind IN ('v', 'm')  -- 'v' = view, 'm' = materialized view
ORDER BY c.relname;
```

**Model Class Structure:**
```csharp
public class PgView
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public ViewStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public bool IsMaterialized { get; set; }
    public List<PgPrivilege> Privileges { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}
```

**Method Signature:**
```csharp
private async Task<List<PgView>> ExtractViewsAsync(string schemaName)
{
    var views = new List<PgView>();
    // Implementation here
    return views;
}
```

#### Testing Requirements

**Unit Tests (Coverage Target: ?95%):**
- [ ] `ExtractViewsAsync_ValidSchema_ReturnsViews`
- [ ] `ExtractViewsAsync_EmptySchema_ReturnsEmptyList`
- [ ] `ExtractViewsAsync_NullSchema_ThrowsArgumentNullException`
- [ ] `ExtractViewsAsync_MaterializedView_SetsFlagCorrectly`
- [ ] `ExtractViewsAsync_ViewWithDependencies_TracksDependencies`
- [ ] `ParseViewAst_ValidViewDefinition_ParsesCorrectly`
- [ ] `ParseViewAst_InvalidSql_ThrowsException`
- [ ] `ExtractViewPrivileges_WithGrants_ExtractsCorrectly`
- [ ] `ExtractViewPrivileges_NullAcl_ReturnsEmptyList`

**Integration Tests:**
- [ ] `ExtractViews_FromPostgreSQL12_ExtractsAllViews`
- [ ] `ExtractViews_FromPostgreSQL16_ExtractsAllViews`
- [ ] `ExtractViews_WithComplexJoins_ExtractsCorrectly`
- [ ] `ExtractViews_MaterializedView_ExtractsWithAllProperties`
- [ ] `ExtractViews_ViewReferencingOtherView_TracksDependency`
- [ ] `ExtractViews_WithCTE_ParsesCorrectly`

**Test Data:**
- Create SQL fixtures in `tests/Fixtures/views/`
- Include regular views, materialized views, complex views
- Include views with dependencies on tables and other views

#### Dependencies

- **Blocked by:** Issue #7 (Fix Privilege Extraction Bug) - should fix privileges first
- **Blocks:** Issue #12 (Schema Comparison) - need views for comparison
- **Related to:** Issue #2 (Functions), Issue #8 (Model Enhancement)

#### Labels

`enhancement`, `extraction`, `high-priority`, `mvp`, `good-first-issue`

#### Definition of Done

- [ ] Code compiles without errors
- [ ] All unit tests pass
- [ ] Integration test passes with Testcontainers
- [ ] Views appear correctly in extracted `PgProject` JSON
- [ ] View definitions can be parsed by Npgquery
- [ ] Code review completed
- [ ] Documentation updated (XML comments)

---

### Issue #2: Implement Function Extraction from PostgreSQL Database

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Extraction  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.1.0

#### Description

Function extraction is currently commented out in `PgProjectExtractor.cs`. We need to implement complete extraction of PostgreSQL functions including signatures, bodies, dependencies, and metadata (language, volatility, security settings, etc.).

**Current State:**
```csharp
// Line ~66 in PgProjectExtractor.cs
//pgSchema.Functions.AddRange(await ExtractFunctionsAsync(schema.Name));
```

**Why This Matters:**
Functions are critical database objects in PostgreSQL, often containing core business logic. Without extraction support, users cannot fully migrate or version control their database schemas.

#### Acceptance Criteria

##### Extraction Requirements
- [ ] Implement `ExtractFunctionsAsync(string schemaName)` method in `PgProjectExtractor.cs`
- [ ] Extract function signature (name, parameters, return type)
- [ ] Extract function body/definition using `pg_get_functiondef()`
- [ ] Extract function owner from `pg_roles`

##### Metadata Extraction
- [ ] Extract function language (SQL, plpgsql, C, Python, etc.)
- [ ] Extract volatility setting (IMMUTABLE, STABLE, VOLATILE)
- [ ] Extract security setting (SECURITY DEFINER vs SECURITY INVOKER)
- [ ] Extract strictness (RETURNS NULL ON NULL INPUT / CALLED ON NULL INPUT)
- [ ] Extract cost estimate
- [ ] Extract rows estimate (for set-returning functions)
- [ ] Extract function privileges (EXECUTE grants)

##### Model & Parsing
- [ ] Parse function definition into AST using mbulava-org.Npgquery
- [ ] Create `PgFunction` model class in `Models/DbObjects.cs`
- [ ] Handle overloaded functions (same name, different signatures)
- [ ] Extract function dependencies (tables, types, other functions used)
- [ ] Store functions in `PgSchema.Functions` collection

#### Technical Implementation

**SQL Query to Extract Functions:**
```sql
SELECT 
    p.proname AS function_name,
    r.rolname AS owner,
    pg_get_functiondef(p.oid) AS definition,
    l.lanname AS language,
    CASE p.provolatile
        WHEN 'i' THEN 'IMMUTABLE'
        WHEN 's' THEN 'STABLE'
        WHEN 'v' THEN 'VOLATILE'
    END AS volatility,
    p.prosecdef AS security_definer,
    p.proisstrict AS is_strict,
    p.procost AS cost,
    p.prorows AS estimated_rows,
    pg_get_function_identity_arguments(p.oid) AS identity_args,
    pg_get_function_result(p.oid) AS return_type
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
JOIN pg_roles r ON r.oid = p.proowner
JOIN pg_language l ON l.oid = p.prolang
WHERE n.nspname = @schema
  AND p.prokind = 'f'  -- 'f' = function (not procedure)
ORDER BY p.proname, identity_args;
```

**Model Class Structure:**
```csharp
public class PgFunction
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string IdentityArguments { get; set; } = string.Empty; // For overload resolution
    public string ReturnType { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Volatility { get; set; } = string.Empty;
    public bool IsSecurityDefiner { get; set; }
    public bool IsStrict { get; set; }
    public decimal Cost { get; set; }
    public decimal? EstimatedRows { get; set; }
    public CreateFunctionStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public List<PgPrivilege> Privileges { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}
```

#### Testing Requirements

**Unit Tests (Coverage Target: ?95%):**
- [ ] `ExtractFunctionsAsync_ValidSchema_ReturnsFunctions`
- [ ] `ExtractFunctionsAsync_EmptySchema_ReturnsEmptyList`
- [ ] `ExtractFunctionsAsync_NullSchema_ThrowsArgumentNullException`
- [ ] `ExtractFunctionsAsync_OverloadedFunctions_ReturnsAllOverloads`
- [ ] `ExtractFunctionsAsync_DifferentLanguages_ExtractsAllLanguages`
- [ ] `ExtractFunctionsAsync_VolatilitySettings_ExtractsCorrectly`
- [ ] `ExtractFunctionsAsync_SecurityDefiner_FlagSetCorrectly`
- [ ] `ExtractFunctionsAsync_IsStrict_FlagSetCorrectly`
- [ ] `ParseFunctionMetadata_CostAndRows_ParsedCorrectly`
- [ ] `ParseFunctionAst_ValidFunction_ParsesCorrectly`
- [ ] `DistinguishOverloads_SameNameDifferentParams_DistinguishesCorrectly`

**Integration Tests:**
- [ ] `ExtractFunctions_SQLFunction_ExtractsCorrectly`
- [ ] `ExtractFunctions_PlPgSqlFunction_ExtractsCorrectly`
- [ ] `ExtractFunctions_PythonFunction_ExtractsCorrectly`
- [ ] `ExtractFunctions_OverloadedFunctions_ExtractsAllVariants`
- [ ] `ExtractFunctions_AcrossPostgreSQLVersions_ConsistentResults`
- [ ] `ExtractFunctions_WithComplexReturnTypes_ParsesCorrectly`
- [ ] `ExtractFunctions_SetReturningFunction_ExtractsRowsEstimate`

**Test Data:**
- Create SQL fixtures with functions in different languages
- Include overloaded functions (same name, different signatures)
- Include functions with all volatility settings
- Include SECURITY DEFINER and SECURITY INVOKER examples

#### Example Test Functions

```sql
-- Simple SQL function
CREATE FUNCTION calculate_total(quantity INT, price NUMERIC)
RETURNS NUMERIC
LANGUAGE SQL
IMMUTABLE
AS $$
    SELECT quantity * price;
$$;

-- PL/pgSQL function with security definer
CREATE FUNCTION get_customer_balance(customer_id INT)
RETURNS NUMERIC
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
AS $$
DECLARE
    balance NUMERIC;
BEGIN
    SELECT SUM(amount) INTO balance 
    FROM orders 
    WHERE customer_id = $1;
    RETURN COALESCE(balance, 0);
END;
$$;

-- Overloaded function
CREATE FUNCTION format_name(first_name TEXT)
RETURNS TEXT AS $$ SELECT first_name; $$ LANGUAGE SQL;

CREATE FUNCTION format_name(first_name TEXT, last_name TEXT)
RETURNS TEXT AS $$ SELECT first_name || ' ' || last_name; $$ LANGUAGE SQL;
```

#### Dependencies

- **Blocked by:** Issue #7 (Fix Privilege Extraction)
- **Blocks:** Issue #12 (Schema Comparison)
- **Related to:** Issue #3 (Stored Procedures), Issue #4 (Triggers)

#### Labels

`enhancement`, `extraction`, `high-priority`, `mvp`

#### Definition of Done

- [ ] Code compiles without errors
- [ ] All unit tests pass (including overload scenarios)
- [ ] Integration tests pass with Testcontainers
- [ ] Functions extracted correctly in all test scenarios
- [ ] Function metadata (language, volatility, etc.) captured
- [ ] Overloaded functions distinguished correctly
- [ ] Code review completed
- [ ] XML documentation added

---

### Issue #3: Implement Stored Procedure Extraction (PostgreSQL 11+)

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Extraction  
**Story Points:** 3 (reduced from 5 - no version checking needed)  
**Estimated Time:** 1 day  
**Target Version:** v0.1.0

#### Description

PostgreSQL procedures support transaction control (COMMIT/ROLLBACK) and are different from functions. Since we only support PostgreSQL 16+, procedures are always available.

**Context:**
- Procedures were added in PostgreSQL 11
- Identified by `prokind = 'p'` in `pg_proc`
- Can perform transaction control (unlike functions)
- Cannot return values (use OUT parameters instead)
- **Simplified:** No version checking needed (PostgreSQL 16+ guaranteed)

#### Acceptance Criteria

##### Core Extraction
- [ ] Implement `ExtractProceduresAsync(string schemaName)` method
- [ ] Extract procedure signature (name, parameters)
- [ ] Extract procedure body using `pg_get_functiondef()`
- [ ] Extract procedure owner
- [ ] Extract procedure language

##### Model & Storage
- [ ] Create `PgProcedure` model class in `Models/DbObjects.cs`
- [ ] Parse procedure definition into AST
- [ ] Handle procedures with transaction control (COMMIT/ROLLBACK)
- [ ] Extract procedure privileges (EXECUTE grants)
- [ ] Store in `PgSchema.Procedures` collection


#### Technical Implementation

**SQL Query to Extract Procedures:**
```sql
SELECT 
    p.proname AS procedure_name,
    r.rolname AS owner,
    pg_get_functiondef(p.oid) AS definition,
    l.lanname AS language,
    pg_get_function_identity_arguments(p.oid) AS identity_args
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
JOIN pg_roles r ON r.oid = p.proowner
JOIN pg_language l ON l.oid = p.prolang
WHERE n.nspname = @schema
  AND p.prokind = 'p'  -- 'p' = procedure
ORDER BY p.proname;
```

**Note:** No version checking needed - PostgreSQL 16+ guaranteed to support procedures.

**Model Class:**
```csharp
public class PgProcedure
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string IdentityArguments { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public CreateProcedureStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public List<PgPrivilege> Privileges { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}
```

#### Testing Requirements

- [ ] Unit test for `ExtractProceduresAsync`
- [ ] Integration test with PostgreSQL 16:
  - Create simple procedure
  - Create procedure with OUT parameters
  - Create procedure with transaction control
  - Extract and verify
- [ ] Test privilege extraction for procedures

**Note:** No multi-version testing needed - PostgreSQL 16+ only.

#### Example Test Procedures

```sql
-- Simple procedure (PostgreSQL 11+)
CREATE PROCEDURE insert_customer(
    name TEXT,
    email TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO customers (name, email) VALUES (name, email);
    COMMIT;
END;
$$;

-- Procedure with OUT parameters
CREATE PROCEDURE get_stats(
    OUT total_customers INT,
    OUT total_orders INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT COUNT(*) INTO total_customers FROM customers;
    SELECT COUNT(*) INTO total_orders FROM orders;
END;
$$;

-- Procedure with transaction control
CREATE PROCEDURE process_batch()
LANGUAGE plpgsql
AS $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN SELECT * FROM pending_orders LOOP
        BEGIN
            INSERT INTO processed_orders SELECT * FROM pending_orders WHERE id = r.id;
            DELETE FROM pending_orders WHERE id = r.id;
            COMMIT;
        EXCEPTION
            WHEN OTHERS THEN
                ROLLBACK;
                INSERT INTO error_log VALUES (r.id, SQLERRM);
                COMMIT;
        END;
    END LOOP;
END;
$$;
```

#### Dependencies

- **Blocked by:** Issue #7 (Fix Privilege Extraction)
- **Related to:** Issue #2 (Functions)
- **Blocks:** Issue #12 (Schema Comparison)

#### Labels

`enhancement`, `extraction`, `high-priority`, `mvp`

#### Definition of Done

- [ ] Code compiles without errors
- [ ] Procedures extracted on PostgreSQL 16+
- [ ] All tests pass
- [ ] Procedures with transaction control handled
- [ ] Code review completed
- [ ] Documentation updated

---

### Issue #4: Implement Trigger Extraction from PostgreSQL Database

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Extraction  
**Story Points:** 5  
**Estimated Time:** 1-2 days  
**Target Version:** v0.1.0

#### Description

Trigger extraction is currently commented out. Triggers are essential for capturing business logic tied to table events (INSERT, UPDATE, DELETE, TRUNCATE).

**Current State:**
```csharp
// Line ~67 in PgProjectExtractor.cs
//pgSchema.Triggers.AddRange(await ExtractTriggersAsync(schema.Name));
```

#### Acceptance Criteria

##### Core Extraction
- [ ] Implement `ExtractTriggersAsync(string schemaName)` method
- [ ] Extract trigger name
- [ ] Extract table/view name the trigger is on
- [ ] Extract trigger definition using `pg_get_triggerdef()`
- [ ] Parse trigger definition into AST

##### Metadata Extraction
- [ ] Extract trigger timing (BEFORE, AFTER, INSTEAD OF)
- [ ] Extract trigger events (INSERT, UPDATE, DELETE, TRUNCATE, or combinations)
- [ ] Extract for each row vs statement level
- [ ] Extract WHEN condition (if present)
- [ ] Extract function called by trigger
- [ ] Extract trigger enabled/disabled state

##### Model & Storage
- [ ] Create `PgTrigger` model class
- [ ] Handle triggers on tables
- [ ] Handle triggers on views (INSTEAD OF triggers)
- [ ] Handle event triggers separately (database-wide triggers)
- [ ] Store in `PgSchema.Triggers` or `PgTable.Triggers` collection
- [ ] Track dependencies (trigger function, target table)

#### Technical Implementation

**SQL Query to Extract Triggers:**
```sql
SELECT 
    tg.tgname AS trigger_name,
    c.relname AS table_name,
    ns.nspname AS table_schema,
    pg_get_triggerdef(tg.oid) AS definition,
    p.proname AS function_name,
    tg.tgenabled AS is_enabled,
    -- Decode trigger type bitmap
    (tg.tgtype & 2) != 0 AS is_before,
    (tg.tgtype & 4) != 0 AS is_after,
    (tg.tgtype & 64) != 0 AS is_instead,
    (tg.tgtype & 4) != 0 AS fires_on_insert,
    (tg.tgtype & 8) != 0 AS fires_on_delete,
    (tg.tgtype & 16) != 0 AS fires_on_update,
    (tg.tgtype & 32) != 0 AS fires_on_truncate,
    (tg.tgtype & 1) != 0 AS is_row_trigger,
    tg.tgqual AS when_condition
FROM pg_trigger tg
JOIN pg_class c ON c.oid = tg.tgrelid
JOIN pg_namespace ns ON ns.oid = c.relnamespace
LEFT JOIN pg_proc p ON p.oid = tg.tgfoid
WHERE ns.nspname = @schema
  AND NOT tg.tgisinternal
ORDER BY c.relname, tg.tgname;
```

**Model Class:**
```csharp
public class PgTrigger
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    
    public bool IsBefore { get; set; }
    public bool IsAfter { get; set; }
    public bool IsInsteadOf { get; set; }
    
    public bool FiresOnInsert { get; set; }
    public bool FiresOnUpdate { get; set; }
    public bool FiresOnDelete { get; set; }
    public bool FiresOnTruncate { get; set; }
    
    public bool IsRowTrigger { get; set; }
    
    public string? WhenCondition { get; set; }
    public CreateTrigStmt? Ast { get; set; }
    public string? AstJson { get; set; }
    public List<string> Dependencies { get; set; } = new();
}
```

#### Testing Requirements

- [ ] Unit test for trigger type decoding (bitmap)
- [ ] Integration test with Testcontainers (multiple trigger scenarios)
- [ ] Test disabled triggers
- [ ] Test triggers with complex functions
- [ ] Test dependency tracking

#### Dependencies

- **Depends on:** Issue #2 (Functions must be extracted first)
- **Blocks:** Issue #12 (Schema Comparison)
- **Related to:** Issue #1 (Views - for INSTEAD OF triggers)

#### Labels

`enhancement`, `extraction`, `high-priority`, `mvp`

#### Definition of Done

- [ ] Code compiles without errors
- [ ] Trigger type bitmap decoded correctly
- [ ] All trigger timings handled
- [ ] All tests pass
- [ ] Triggers correctly associated with tables/views
- [ ] Code review completed
- [ ] Documentation updated

---

### Issue #5: Implement Index Extraction from PostgreSQL Database

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Extraction  
**Story Points:** 5  
**Estimated Time:** 2 days  
**Target Version:** v0.1.0

#### Description

Indexes are critical for database performance and need to be extracted with full metadata including type, columns, expressions, WHERE clauses, and storage options.

#### Acceptance Criteria

##### Core Extraction
- [ ] Implement `ExtractIndexesAsync(string schemaName)` method
- [ ] Extract index name
- [ ] Extract table name
- [ ] Extract index definition using `pg_get_indexdef()`
- [ ] Parse index definition into AST

##### Metadata Extraction
- [ ] Extract index type (btree, hash, gist, gin, brin, spgist, bloom)
- [ ] Extract unique constraint flag
- [ ] Extract primary key flag
- [ ] Extract columns (for column-based indexes)
- [ ] Extract expressions (for expression indexes)
- [ ] Extract INCLUDE columns (covering indexes)
- [ ] Extract WHERE clause (partial indexes)
- [ ] Extract tablespace
- [ ] Extract index options (fillfactor, etc.)
- [ ] Extract index size

##### Model & Storage
- [ ] Create `PgIndex` model class
- [ ] Handle expression indexes
- [ ] Handle multi-column indexes
- [ ] Store in `PgTable.Indexes` collection

#### Technical Implementation

**SQL Query:**
```sql
SELECT 
    i.relname AS index_name,
    t.relname AS table_name,
    ns.nspname AS schema_name,
    am.amname AS index_type,
    idx.indisunique AS is_unique,
    idx.indisprimary AS is_primary,
    pg_get_indexdef(i.oid, 0, true) AS definition,
    idx.indpred IS NOT NULL AS has_predicate,
    pg_get_expr(idx.indpred, idx.indrelid) AS predicate,
    ts.spcname AS tablespace,
    pg_relation_size(i.oid) AS size_bytes
FROM pg_index idx
JOIN pg_class i ON i.oid = idx.indexrelid
JOIN pg_class t ON t.oid = idx.indrelid
JOIN pg_namespace ns ON ns.oid = t.relnamespace
JOIN pg_am am ON am.oid = i.relam
LEFT JOIN pg_tablespace ts ON ts.oid = i.reltablespace
WHERE ns.nspname = @schema
ORDER BY t.relname, i.relname;
```

**Model Class:**
```csharp
public class PgIndex
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string IndexType { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsPrimary { get; set; }
    public List<string> KeyColumns { get; set; } = new();
    public List<string> IncludeColumns { get; set; } = new();
    public bool HasPredicate { get; set; }
    public string? Predicate { get; set; }
    public string? Tablespace { get; set; }
    public long SizeBytes { get; set; }
    public Dictionary<string, string> Options { get; set; } = new();
}
```

#### Testing Requirements

- [ ] Unit test for `ExtractIndexesAsync`
- [ ] Integration test with various index types
- [ ] Test expression indexes
- [ ] Test partial indexes
- [ ] Test covering indexes (INCLUDE)

#### Dependencies

- **Blocks:** Issue #12 (Schema Comparison)
- **Related to:** Issue #6 (Constraints)
- **Enhances:** Issue #8 (PgTable Model)

#### Labels

`enhancement`, `extraction`, `high-priority`, `mvp`

#### Definition of Done

- [ ] All index types extracted
- [ ] Expression indexes handled
- [ ] Partial indexes preserved
- [ ] All tests pass
- [ ] Code review completed

---

### Issue #6: Implement Constraint Extraction (FK, Check, Unique, Exclusion)

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Extraction  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.1.0

#### Description

Extract all constraint types from PostgreSQL including foreign keys, check constraints, unique constraints, and exclusion constraints.

#### Acceptance Criteria

##### Foreign Key Constraints
- [ ] Extract constraint name
- [ ] Extract source table and columns
- [ ] Extract referenced table and columns
- [ ] Extract ON DELETE/UPDATE actions
- [ ] Extract deferrable settings

##### Check Constraints
- [ ] Extract constraint name
- [ ] Extract check expression
- [ ] Extract validated state (NOT VALID)

##### Unique Constraints
- [ ] Extract constraint name
- [ ] Extract columns
- [ ] Distinguish from unique indexes

##### Exclusion Constraints
- [ ] Extract PostgreSQL-specific exclusion constraints
- [ ] Extract operator and columns

##### Model & Storage
- [ ] Create constraint model classes:
  - `PgForeignKeyConstraint`
  - `PgCheckConstraint`
  - `PgUniqueConstraint`
  - `PgExclusionConstraint`
- [ ] Parse constraint definitions into AST
- [ ] Store in appropriate collections on `PgTable`

#### Technical Implementation

**SQL Query:**
```sql
SELECT 
    con.conname AS constraint_name,
    con.contype AS constraint_type,  -- p=primary, f=foreign, u=unique, c=check, x=exclusion
    t.relname AS table_name,
    pg_get_constraintdef(con.oid) AS definition
FROM pg_constraint con
JOIN pg_class t ON t.oid = con.conrelid
JOIN pg_namespace n ON n.oid = t.relnamespace
WHERE n.nspname = @schema
ORDER BY t.relname, con.conname;
```

**Model Classes:**
```csharp
public class PgForeignKeyConstraint
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public string ReferencedTable { get; set; } = string.Empty;
    public List<string> ReferencedColumns { get; set; } = new();
    public string OnDelete { get; set; } = string.Empty;
    public string OnUpdate { get; set; } = string.Empty;
    public bool IsDeferrable { get; set; }
    public bool InitiallyDeferred { get; set; }
}

public class PgCheckConstraint
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public bool IsValidated { get; set; } = true;
}

public class PgUniqueConstraint
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
}

public class PgExclusionConstraint
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}
```

#### Testing Requirements

- [ ] Test foreign key extraction
- [ ] Test check constraint extraction
- [ ] Test unique constraint extraction
- [ ] Test exclusion constraint extraction
- [ ] Test constraint validation states

#### Dependencies

- **Related to:** Issue #5 (Indexes)
- **Enhances:** Issue #8 (PgTable Model)
- **Blocks:** Issue #12 (Schema Comparison)

#### Labels

`enhancement`, `extraction`, `high-priority`, `mvp`

#### Definition of Done

- [ ] All constraint types extracted
- [ ] Foreign key actions captured
- [ ] Check expressions preserved
- [ ] All tests pass
- [ ] Code review completed

---

### Issue #7: Fix Privilege Extraction Bug

**Status:** ?? Not Started  
**Priority:** P0 - Critical (Blocker)  
**Component:** Extraction, Bug Fix  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.1.0

#### Description

Schema privilege extraction is currently failing (marked as BUG in code). Need to debug and fix the ACL parsing for all object types.

**Context:**
From `PgProjectExtractor.cs` line 113:
```csharp
//BUG: Privileges extraction fails here, might be a public thing though - needs investigation
//Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
```

#### Acceptance Criteria

##### Core Fixes
- [ ] Investigate why schema privilege extraction fails
- [ ] Fix ACL array parsing in `ExtractPrivilegesAsync` method
- [ ] Handle `NULL` ACL (default public privileges)
- [ ] Handle empty ACL arrays
- [ ] Parse ACL format correctly: `grantee=privileges/grantor`

##### ACL Privilege Code Mapping
- [ ] Map single-character privilege codes to names:
  - `r` = SELECT
  - `w` = UPDATE
  - `a` = INSERT
  - `d` = DELETE
  - `D` = TRUNCATE
  - `x` = REFERENCES
  - `t` = TRIGGER
  - `X` = EXECUTE
  - `U` = USAGE
  - `C` = CREATE
  - `c` = CONNECT
  - `T` = TEMPORARY
- [ ] Handle grant options (uppercase letters in ACL)
- [ ] Handle PUBLIC grants (empty grantee)

##### Test All Object Types
- [ ] Test privilege extraction for schemas
- [ ] Test privilege extraction for tables
- [ ] Test privilege extraction for views
- [ ] Test privilege extraction for functions
- [ ] Test privilege extraction for procedures
- [ ] Test privilege extraction for sequences
- [ ] Add unit tests for ACL parsing edge cases

#### Technical Notes

**ACL Format Examples:**
- `postgres=arwdDxt/postgres` - user postgres has all privileges
- `=r/postgres` - PUBLIC has SELECT
- `user1=ar*/postgres` - user1 has INSERT, SELECT with grant option

**Fixed Method Signature:**
```csharp
private async Task<List<PgPrivilege>> ExtractPrivilegesAsync(
    string sql, 
    string paramName, 
    object paramValue)
{
    var privileges = new List<PgPrivilege>();
    
    using var cmd = new NpgsqlCommand(sql, CreateConnection());
    cmd.Parameters.AddWithValue(paramName, paramValue);
    
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return privileges; // No results
    
    // Handle NULL ACL
    if (reader.IsDBNull(0))
        return privileges; // Default privileges
    
    var aclArray = reader.GetFieldValue<string[]>(0);
    
    foreach (var acl in aclArray)
    {
        // Parse: grantee=privileges/grantor
        var parts = acl.Split('=');
        if (parts.Length < 2) continue;
        
        var grantee = string.IsNullOrEmpty(parts[0]) ? "PUBLIC" : parts[0];
        var rightsAndGrantor = parts[1].Split('/');
        var rights = rightsAndGrantor[0];
        var grantor = rightsAndGrantor.Length > 1 ? rightsAndGrantor[1] : string.Empty;
        
        foreach (var ch in rights)
        {
            var privilege = new PgPrivilege
            {
                Grantee = grantee,
                Grantor = grantor,
                PrivilegeType = MapPrivilegeCode(ch, out bool isGrantable),
                IsGrantable = isGrantable
            };
            privileges.Add(privilege);
        }
    }
    
    return privileges;
}

private string MapPrivilegeCode(char code, out bool isGrantable)
{
    isGrantable = char.IsUpper(code);
    char lower = char.ToLower(code);
    
    return lower switch
    {
        'r' => "SELECT",
        'w' => "UPDATE",
        'a' => "INSERT",
        'd' => "DELETE",
        'x' => "REFERENCES",
        't' => "TRIGGER",
        'X' => "EXECUTE",
        'U' => "USAGE",
        'C' => "CREATE",
        'c' => "CONNECT",
        'T' => "TEMPORARY",
        'D' => "TRUNCATE",
        _ => $"UNKNOWN_{code}"
    };
}
```

#### Testing Requirements

- [ ] Unit tests for ACL parsing:
  - Parse NULL ACL
  - Parse empty ACL array
  - Parse simple privilege: `postgres=arwdDxt/postgres`
  - Parse PUBLIC privilege: `=r/postgres`
  - Parse grant option: `user1=ar*/postgres`
  - Parse multiple grantees
- [ ] Integration tests:
  - Create schema with custom privileges
  - Create table with column-level privileges
  - Create function with EXECUTE privileges
  - Extract and verify all privileges

#### Dependencies

- **Blocks:** All extraction issues (#1-6)
- **Critical:** Must be fixed first for complete extraction

#### Labels

`bug`, `extraction`, `high-priority`, `mvp`, `blocker`

#### Definition of Done

- [ ] Privilege extraction works for all object types
- [ ] ACL parsing handles all edge cases
- [ ] All privilege codes mapped correctly
- [ ] Grant options handled
- [ ] PUBLIC grants handled
- [ ] All tests pass
- [ ] Code review completed
- [ ] Documentation updated

---

### Issue #8: Enhance PgTable Model Properties

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Model  
**Story Points:** 5  
**Estimated Time:** 1-2 days  
**Target Version:** v0.1.0

#### Description

`PgTable` model needs additional properties to fully represent PostgreSQL tables including indexes, constraints, statistics, and options.

#### Acceptance Criteria

##### Table-Level Properties
- [ ] Add missing properties to `PgTable` class:
  - `Indexes` (List<PgIndex>)
  - `ForeignKeys` (List<PgForeignKeyConstraint>)
  - `CheckConstraints` (List<PgCheckConstraint>)
  - `UniqueConstraints` (List<PgUniqueConstraint>)
  - `ExclusionConstraints` (List<PgExclusionConstraint>)
  - `Tablespace` (string)
  - `HasOids` (bool - deprecated but still used)
  - `FillFactor` (int?)
  - `AutovacuumEnabled` (bool?)
  - `Statistics` (table statistics - optional)
  - `Inheritance` (List<string> - inherited tables)
  - `Partitioning` (partitioning info if partitioned)
  - `RowLevelSecurity` (bool)
  - `ForceRowLevelSecurity` (bool)

##### Column-Level Model
- [ ] Add `PgColumn` class with full metadata:
  - `Name` (string)
  - `DataType` (string)
  - `IsNullable` (bool)
  - `DefaultValue` (string)
  - `IsIdentity` (bool)
  - `IdentityGeneration` (ALWAYS or BY DEFAULT)
  - `IsGenerated` (bool - computed columns)
  - `GenerationExpression` (string)
  - `Collation` (string)
  - `ColumnComment` (string)
  - `Position` (int - ordinal position)

##### Implementation
- [ ] Update table extraction to populate all properties
- [ ] Ensure serialization/deserialization works for complex types
- [ ] Add validation for required properties

#### Model Classes

```csharp
public class PgTable
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string? Tablespace { get; set; }
    
    // Columns
    public List<PgColumn> Columns { get; set; } = new();
    
    // Indexes and Constraints
    public List<PgIndex> Indexes { get; set; } = new();
    public List<PgForeignKeyConstraint> ForeignKeys { get; set; } = new();
    public List<PgCheckConstraint> CheckConstraints { get; set; } = new();
    public List<PgUniqueConstraint> UniqueConstraints { get; set; } = new();
    public List<PgExclusionConstraint> ExclusionConstraints { get; set; } = new();
    
    // Privileges
    public List<PgPrivilege> Privileges { get; set; } = new();
    
    // Table Options
    public bool HasOids { get; set; }
    public int? FillFactor { get; set; }
    public bool? AutovacuumEnabled { get; set; }
    
    // Inheritance
    public List<string> InheritsFrom { get; set; } = new();
    
    // Partitioning
    public PgPartitionInfo? PartitionInfo { get; set; }
    
    // Row-Level Security
    public bool RowLevelSecurity { get; set; }
    public bool ForceRowLevelSecurity { get; set; }
    
    // AST
    public CreateStmt? Ast { get; set; }
    public string? AstJson { get; set; }
}

public class PgColumn
{
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
    
    // Identity columns (PostgreSQL 10+)
    public bool IsIdentity { get; set; }
    public string? IdentityGeneration { get; set; } // ALWAYS or BY DEFAULT
    
    // Generated columns (PostgreSQL 12+)
    public bool IsGenerated { get; set; }
    public string? GenerationExpression { get; set; }
    
    // Other properties
    public string? Collation { get; set; }
    public string? Comment { get; set; }
}

public class PgPartitionInfo
{
    public string Strategy { get; set; } = string.Empty; // RANGE, LIST, HASH
    public List<string> PartitionKeys { get; set; } = new();
    public bool IsPartitioned { get; set; }
    public string? ParentTable { get; set; }
}
```

#### Testing Requirements

- [ ] Unit test for model serialization/deserialization
- [ ] Test with table with all properties set
- [ ] Test with minimal table (only required properties)
- [ ] Test partitioned tables
- [ ] Test inherited tables

#### Dependencies

- **Enhanced by:** Issue #5 (Indexes), Issue #6 (Constraints)
- **Blocks:** Issue #12 (Schema Comparison)

#### Labels

`enhancement`, `model`, `high-priority`, `mvp`

#### Definition of Done

- [ ] All properties added
- [ ] Model validates correctly
- [ ] Serialization works
- [ ] Tests pass
- [ ] Documentation updated

---

### Issue #9: Implement Compiler Reference Validation

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Compilation  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.2.0

#### Description

`ProjectCompiler.Validate()` currently has commented-out reference validation. Need to implement full validation of object references across SQL files.

#### Acceptance Criteria

##### Extract References from AST
- [ ] Implement `ExtractReferences(ParseResult)` method to walk AST and collect:
  - Table references
  - View references
  - Function/procedure calls
  - Type references
  - Sequence references (nextval, currval, setval)
  - Schema references
  - Column references

##### Build Project Catalog
- [ ] Build in-memory catalog of all objects defined in project
- [ ] Index objects by qualified name (schema.object)
- [ ] Index objects by unqualified name for search path resolution

##### Validate References
- [ ] Implement `ReferenceExists(string reference)` to check against catalog
- [ ] Support qualified names (`schema.object`)
- [ ] Support unqualified names (search path resolution)
- [ ] Handle cross-schema references
- [ ] Support references to objects in PackageReference/ProjectReference
- [ ] Handle SQLCMD variables in references

##### Error Generation
- [ ] Generate compiler errors for:
  - Missing table references
  - Missing column references
  - Missing function references
  - Missing type references
  - Missing sequence references
- [ ] Include file name and line number in errors
- [ ] Add warnings for potential issues

#### Technical Implementation

```csharp
public class ProjectCompiler
{
    private readonly Dictionary<string, DbObject> _catalog = new();
    
    public CompilerResult Validate()
    {
        var result = new CompilerResult();
        
        // Build catalog first
        BuildCatalog();
        
        foreach (var file in Directory.EnumerateFiles(_projectPath, "*.sql", SearchOption.AllDirectories))
        {
            var sql = File.ReadAllText(file);
            var parseResult = _parser.Parse(sql);
            
            if (parseResult.IsError)
            {
                result.Errors.Add(new CompilerError(file, "Parse error", parseResult.Error));
                continue;
            }
            
            // Extract and validate references
            var references = ExtractReferences(parseResult);
            foreach (var reference in references)
            {
                if (!ReferenceExists(reference))
                {
                    result.Errors.Add(new CompilerError(
                        file, 
                        $"Reference to undefined object: {reference}", 
                        reference.LineNumber));
                }
            }
        }
        
        return result;
    }
    
    private void BuildCatalog()
    {
        // Scan all SQL files and extract object definitions
        // Add to _catalog dictionary
    }
    
    private List<ObjectReference> ExtractReferences(ParseResult parseResult)
    {
        var references = new List<ObjectReference>();
        
        // Walk AST to find all references
        // - RangeVar nodes for table/view references
        // - FuncCall nodes for function calls
        // - TypeName nodes for type references
        
        return references;
    }
    
    private bool ReferenceExists(ObjectReference reference)
    {
        // Check in project catalog
        if (_catalog.ContainsKey(reference.QualifiedName))
            return true;
        
        // Check in package references
        // Check in system catalogs (pg_catalog, information_schema)
        
        return false;
    }
}

public class ObjectReference
{
    public string Name { get; set; }
    public string? Schema { get; set; }
    public string QualifiedName => Schema != null ? $"{Schema}.{Name}" : Name;
    public ReferenceType Type { get; set; }
    public int LineNumber { get; set; }
}

public enum ReferenceType
{
    Table,
    View,
    Function,
    Procedure,
    Type,
    Sequence,
    Schema
}
```

#### Example Validation Scenarios

```sql
-- Should validate that referenced objects exist
CREATE VIEW my_view AS
SELECT * FROM my_table;  -- validates my_table exists

CREATE FUNCTION my_func() RETURNS void AS $$
BEGIN
    PERFORM * FROM other_table;  -- validates other_table exists
    INSERT INTO log_table VALUES (...);  -- validates log_table exists
END;
$$ LANGUAGE plpgsql;

-- Should error on missing reference
CREATE VIEW bad_view AS
SELECT * FROM non_existent_table;  -- ERROR: Reference to undefined object: non_existent_table
```

#### Testing Requirements

- [ ] Test valid SQL files compile successfully
- [ ] Test invalid SQL files produce appropriate errors
- [ ] Test reference validation catches missing dependencies
- [ ] Test qualified vs unqualified references
- [ ] Test cross-schema references
- [ ] Test references to package references

#### Dependencies

- **Related to:** Issue #10 (Circular Dependencies)
- **Blocks:** Issue #12 (Schema Comparison)

#### Labels

`enhancement`, `compilation`, `high-priority`, `mvp`

#### Definition of Done

- [ ] Reference extraction works for all object types
- [ ] Catalog building complete
- [ ] Validation detects missing references
- [ ] Clear error messages with line numbers
- [ ] All tests pass
- [ ] Code review completed

---

### Issue #10: Implement Circular Dependency Detection

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Compilation  
**Story Points:** 5  
**Estimated Time:** 1-2 days  
**Target Version:** v0.2.0

#### Description

Implement dependency graph analysis to detect circular dependencies between database objects and provide clear error messages.

#### Acceptance Criteria

##### Graph Building
- [ ] Build directed dependency graph from all project objects
- [ ] Nodes represent database objects
- [ ] Edges represent dependencies (A depends on B)
- [ ] Handle multiple types of dependencies

##### Cycle Detection
- [ ] Use topological sort to detect cycles
- [ ] Identify all strongly connected components
- [ ] Report full cycle path in error message
- [ ] Report all cycles found (not just first one)

##### Valid Circular Scenarios
- [ ] Handle mutually recursive functions (allowed in PostgreSQL)
- [ ] Handle deferred constraints (not truly circular)
- [ ] Don't flag these as errors

##### Error Reporting
- [ ] Generate clear error messages with full cycle path
- [ ] Provide suggestions to break cycles
- [ ] Include object types in error message

#### Technical Implementation

```csharp
public class DependencyAnalyzer
{
    private readonly Dictionary<string, List<string>> _dependencies = new();
    private readonly Dictionary<string, string> _objectTypes = new();
    
    public List<CircularDependency> DetectCycles()
    {
        var cycles = new List<CircularDependency>();
        
        // Use Tarjan's algorithm to find strongly connected components
        var scc = FindStronglyConnectedComponents();
        
        foreach (var component in scc)
        {
            if (component.Count > 1 || HasSelfLoop(component[0]))
            {
                // Found a cycle
                var cycle = new CircularDependency
                {
                    Objects = component,
                    Path = BuildCyclePath(component)
                };
                cycles.Add(cycle);
            }
        }
        
        return cycles;
    }
    
    private List<List<string>> FindStronglyConnectedComponents()
    {
        // Tarjan's algorithm implementation
        // Returns list of strongly connected components
    }
    
    private bool IsValidCircular(List<string> cycle)
    {
        // Check if this is a valid circular scenario
        // e.g., mutually recursive functions
        if (cycle.All(obj => _objectTypes[obj] == "Function"))
        {
            // Mutually recursive functions are allowed
            return true;
        }
        
        return false;
    }
}

public class CircularDependency
{
    public List<string> Objects { get; set; }
    public string Path { get; set; }
    public string Suggestion { get; set; }
}
```

#### Example Error Message

```
Error: Circular dependency detected:
  view_a (View) -> view_b (View) -> view_c (View) -> view_a (View)
  
Location:
  view_a defined in: Tables/view_a.sql:1
  view_b defined in: Tables/view_b.sql:1
  view_c defined in: Tables/view_c.sql:1

Suggestion: Consider breaking the cycle by:
  - Using materialized views instead of regular views
  - Creating an intermediate view or table
  - Refactoring to remove the circular reference
```

#### Testing Requirements

- [ ] Test simple cycle (A -> B -> A)
- [ ] Test complex cycle (A -> B -> C -> A)
- [ ] Test self-referencing object
- [ ] Test multiple independent cycles
- [ ] Test valid mutually recursive functions (no error)
- [ ] Test mixed object types in cycle

#### Test Scenarios

```sql
-- Simple cycle (should error)
CREATE VIEW view_a AS SELECT * FROM view_b;
CREATE VIEW view_b AS SELECT * FROM view_a;

-- Complex cycle (should error)
CREATE VIEW view_a AS SELECT * FROM view_b;
CREATE VIEW view_b AS SELECT * FROM view_c;
CREATE VIEW view_c AS SELECT * FROM view_a;

-- Valid mutually recursive functions (should NOT error)
CREATE FUNCTION is_even(n INT) RETURNS BOOLEAN AS $$
    SELECT CASE WHEN n = 0 THEN true ELSE is_odd(n - 1) END;
$$ LANGUAGE SQL;

CREATE FUNCTION is_odd(n INT) RETURNS BOOLEAN AS $$
    SELECT CASE WHEN n = 0 THEN false ELSE is_even(n - 1) END;
$$ LANGUAGE SQL;
```

#### Dependencies

- **Depends on:** Issue #9 (Reference Validation)
- **Blocks:** Issue #12 (Schema Comparison)

#### Labels

`enhancement`, `compilation`, `high-priority`, `mvp`

#### Definition of Done

- [ ] Cycle detection algorithm implemented
- [ ] Clear error messages with full paths
- [ ] Valid circular scenarios handled correctly
- [ ] All tests pass
- [ ] Code review completed
- [ ] Documentation updated

---

### Issue #11: Create Integration Tests with Testcontainers

**Status:** ?? Not Started  
**Priority:** P1 - High (MVP)  
**Component:** Testing, Infrastructure  
**Story Points:** 8 (reduced from 13 - single version testing)  
**Estimated Time:** 2-3 days  
**Target Version:** v0.1.0

#### Description

Create comprehensive integration test infrastructure using Testcontainers to test extraction, compilation, and deployment against PostgreSQL 16+.

**Current Problem:**
Existing Testcontainers setup is failing due to PostgreSQL connection issues.

**Simplified Scope:**
Since we only support PostgreSQL 16+, we only need to test against one version, significantly simplifying the test infrastructure.

#### Acceptance Criteria

##### Infrastructure Setup
- [ ] Fix existing Testcontainers setup (currently failing)
- [ ] Create base test class for PostgreSQL tests
- [ ] Configure connection retry logic
- [ ] Set up proper test database initialization

##### PostgreSQL Version Support
- [ ] Create test fixtures for PostgreSQL 16
- [ ] Consider forward compatibility testing with PostgreSQL 17+ (when available)
- [ ] No multi-version testing matrix needed (simplified)

##### Test Databases
- [ ] Create sample test databases with known schema:
  - **Basic Database:** Tables, views, simple functions
  - **Complex Database:** All object types (triggers, procedures, indexes, constraints)
  - **Northwind PostgreSQL:** Popular sample database for realistic testing
- [ ] SQL scripts to seed test data
- [ ] Automated database setup in test fixtures

##### Integration Test Coverage

**Extraction Tests:**
- [ ] Test schema extraction
- [ ] Test table extraction (with all properties)
- [ ] Test view extraction (regular and materialized)
- [ ] Test function extraction (all languages)
- [ ] Test procedure extraction
- [ ] Test trigger extraction
- [ ] Test index extraction (all types)
- [ ] Test constraint extraction (all types)
- [ ] Test privilege extraction (all object types)
- [ ] Test sequence extraction
- [ ] Test type extraction (enums, composites, domains)

**Compilation Tests:**
- [ ] Test valid SQL files compile successfully
- [ ] Test invalid SQL files produce appropriate errors
- [ ] Test reference validation works
- [ ] Test circular dependency detection

**Publishing Tests:**
- [ ] Test deploy to empty database
- [ ] Test deploy updates to existing database
- [ ] Test objects created correctly
- [ ] Test rollback on error

**Upgrade Scenarios:**
- [ ] Test upgrading from version A to B
- [ ] Test schema changes applied correctly

##### CI/CD Integration
- [ ] Configure tests to run in CI/CD pipeline
- [ ] Add test result reporting
- [ ] Configure test timeouts
- [ ] Add performance benchmarks (optional)

#### Technical Implementation

**Base Test Class:**
```csharp
public abstract class PostgreSqlIntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer Container { get; private set; }
    protected string ConnectionString { get; private set; }
    
    public async Task InitializeAsync()
    {
        Container = new PostgreSqlBuilder()
            .WithImage($"postgres:{PostgreSqlVersion}")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
        
        await Container.StartAsync();
        
        // Retry connection
        ConnectionString = Container.GetConnectionString();
        await WaitForDatabaseReady();
        
        // Seed test data
        await SeedTestDatabase();
    }
    
    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
    
    protected async Task WaitForDatabaseReady()
    {
        var maxRetries = 30;
        var delay = TimeSpan.FromSeconds(1);
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                return; // Success
            }
            catch
            {
                if (i == maxRetries - 1) throw;
                await Task.Delay(delay);
            }
        }
    }
    
    protected abstract Task SeedTestDatabase();
    
    protected abstract string PostgreSqlVersion { get; }
}
```

**Example Test:**
```csharp
[TestFixture]
public class ViewExtractionTests : PostgreSqlIntegrationTestBase
{
    protected override string PostgreSqlVersion => "16";
    
    protected override async Task SeedTestDatabase()
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        
        await conn.ExecuteAsync(@"
            CREATE TABLE customers (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT
            );
            
            CREATE VIEW active_customers AS
            SELECT * FROM customers WHERE email IS NOT NULL;
            
            CREATE MATERIALIZED VIEW customer_stats AS
            SELECT COUNT(*) as total FROM customers;
        ");
    }
    
    [Test]
    public async Task Extract_CompleteSchema_Success()
    {
        // Arrange
        var extractor = new PgProjectExtractor(ConnectionString);
        
        // Act
        var project = await extractor.ExtractPgProject("testdb", "16.0");
        
        // Assert
        Assert.That(project.Schemas, Has.Count.EqualTo(1));
        
        var publicSchema = project.Schemas[0];
        Assert.That(publicSchema.Name, Is.EqualTo("public"));
        Assert.That(publicSchema.Tables, Has.Count.EqualTo(1));
        Assert.That(publicSchema.Views, Has.Count.EqualTo(2));
        
        var regularView = publicSchema.Views.First(v => v.Name == "active_customers");
        Assert.That(regularView.IsMaterialized, Is.False);
        
        var materializedView = publicSchema.Views.First(v => v.Name == "customer_stats");
        Assert.That(materializedView.IsMaterialized, Is.True);
    }
}
```

**Parameterized Test for Multiple Versions:**
```csharp
[TestFixture]
[TestCase("12")]
[TestCase("13")]
[TestCase("14")]
[TestCase("15")]
[TestCase("16")]
public class CrossVersionExtractionTests
{
    [Test]
    public async Task Extract_WorksAcrossAllVersions(string version)
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage($"postgres:{version}")
            .Build();
        
        await container.StartAsync();
        // ... test logic
    }
}
```

#### Test Data Organization

```
tests/
??? Fixtures/
?   ??? BasicDatabase.sql
?   ??? ComplexDatabase.sql
?   ??? Northwind.sql
??? Integration/
?   ??? ExtractionTests/
?   ?   ??? ViewExtractionTests.cs
?   ?   ??? FunctionExtractionTests.cs
?   ?   ??? ...
?   ??? CompilationTests/
?   ??? PublishingTests/
??? TestHelpers/
    ??? PostgreSqlTestBase.cs
    ??? TestDataGenerator.cs
```

#### Dependencies

- **Blocked by:** Issue #7 (Fix Privilege Extraction)
- **Validates:** Issues #1-10 (All extraction and compilation features)

#### Labels

`testing`, `infrastructure`, `high-priority`, `mvp`

#### Definition of Done

- [ ] Testcontainers setup fixed and working
- [ ] Tests run against all PostgreSQL versions (12-16)
- [ ] All extraction tests pass
- [ ] All compilation tests pass
- [ ] Publishing tests pass
- [ ] Tests run successfully in CI/CD
- [ ] Test coverage > 80%
- [ ] Documentation on running tests locally
- [ ] Code review completed

---

## Medium Priority Issues

### Issue #12: Complete PgSchemaComparer Implementation

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Comparison  
**Story Points:** 13  
**Estimated Time:** 1 week  
**Target Version:** v0.3.0

#### Description

Complete the implementation of `PgSchemaComparer` to detect all differences between source and target schemas.

#### Acceptance Criteria

- [ ] Complete table comparison implementation (currently incomplete)
- [ ] Implement view comparison
- [ ] Implement function/procedure comparison
- [ ] Implement trigger comparison
- [ ] Implement index comparison
- [ ] Implement constraint comparison
- [ ] Implement extension comparison
- [ ] Implement role comparison
- [ ] Complete sequence comparison with options handling
- [ ] Generate detailed diff objects for each change
- [ ] Include before/after states in diffs
- [ ] Handle renames vs drop+create scenarios
- [ ] Support comparison options (ignore case, ignore comments, etc.)
- [ ] Add unit tests for each comparison type
- [ ] Add integration tests with real databases

#### Dependencies

- **Depends on:** Issues #1-6 (All extraction must be complete)
- **Blocks:** Issue #13 (Script Generation)

#### Labels

`enhancement`, `comparison`, `medium-priority`

---

### Issue #13: Implement PublishScriptGenerator

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Deployment  
**Story Points:** 13  
**Estimated Time:** 1-2 weeks  
**Target Version:** v0.3.0

#### Description

Implement script generation that converts schema differences into executable SQL deployment scripts with proper dependency ordering.

#### Acceptance Criteria

##### Script Generation
- [ ] Generate CREATE scripts for new objects
- [ ] Generate DROP scripts for removed objects
- [ ] Generate ALTER scripts for modified objects
- [ ] Implement topological sort for dependency ordering

##### Object-Specific Scripts
- [ ] Handle schema changes (CREATE/ALTER SCHEMA)
- [ ] Handle table alterations:
  - Add columns
  - Drop columns
  - Modify columns (type, nullability, default)
  - Rename columns (with refactor log support)
- [ ] Handle index changes (DROP/CREATE indexes)
- [ ] Handle constraint changes
- [ ] Handle function/procedure changes (CREATE OR REPLACE)
- [ ] Handle view changes (CREATE OR REPLACE)
- [ ] Handle trigger changes (DROP/CREATE)

##### Safety Features
- [ ] Wrap in transaction where appropriate
- [ ] Add BEGIN/COMMIT/ROLLBACK
- [ ] Generate rollback scripts
- [ ] Detect data-lossy changes and add warnings
- [ ] Support IF EXISTS / IF NOT EXISTS clauses
- [ ] Add safety checks and comments to generated script
- [ ] Generate script header with metadata

#### Example Output

```sql
-- Deployment script generated by pgPacTool
-- Source: MyDatabase v1.0.0
-- Target: production_server
-- Date: 2026-01-31 10:00:00
-- 
-- WARNING: This script contains potentially data-lossy changes
--          Review carefully before executing

BEGIN;

-- Add new column (safe)
ALTER TABLE customers ADD COLUMN email VARCHAR(255);

-- Modify column type (potentially data-lossy)
-- WARNING: Converting numeric(10,2) to integer may lose decimal values
ALTER TABLE orders 
    ALTER COLUMN total_amount TYPE integer 
    USING total_amount::integer;

-- Create new index
CREATE INDEX idx_customers_email ON customers(email);

COMMIT;
```

#### Dependencies

- **Depends on:** Issue #12 (Schema Comparison)
- **Blocks:** Issue #23 (CLI Publish Command)

#### Labels

`enhancement`, `deployment`, `medium-priority`

---

### Issue #14: Complete Attribute Comparer for Column Differences

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Comparison  
**Story Points:** 5  
**Estimated Time:** 1-2 days  
**Target Version:** v0.3.0

#### Description

Complete `PgAttributeComparer` to detect all differences between columns including type changes, nullability, defaults, and constraints.

#### Acceptance Criteria

- [ ] Compare data types (including length, precision, scale)
- [ ] Detect type compatibility issues
- [ ] Compare nullability (NULL vs NOT NULL)
- [ ] Compare default values
- [ ] Compare collations
- [ ] Compare identity column properties
- [ ] Compare generated/computed columns
- [ ] Compare column order/position
- [ ] Compare column comments
- [ ] Detect data-lossy type changes
- [ ] Generate appropriate ALTER COLUMN statements
- [ ] Support type casting expressions when needed
- [ ] Test with all PostgreSQL data types

#### Dependencies

- **Related to:** Issue #12 (Schema Comparer), Issue #13 (Script Generator)

#### Labels

`enhancement`, `comparison`, `medium-priority`

---

### Issue #15: Support Pre/Post Deployment Scripts in DacPackage

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Packaging, Deployment  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.4.0

#### Description

Implement support for pre-deployment and post-deployment scripts that run before/after model deployment, similar to SQL Server SSDT.

#### Acceptance Criteria

- [ ] Support `<PreDeploy>` item in project file
- [ ] Support `<PostDeploy>` item in project file
- [ ] Include scripts in `.pgpac` package
- [ ] Execute pre-deployment script before model changes
- [ ] Execute post-deployment script after model changes
- [ ] Support `:r` include syntax for script composition
- [ ] Support SQLCMD variables in scripts
- [ ] Handle script execution errors appropriately
- [ ] Support `RunScriptsFromReferences` property
- [ ] Execute scripts from referenced packages (when enabled)
- [ ] Log script execution
- [ ] Add script validation during build
- [ ] Test with complex multi-file scripts

#### Example Project File

```xml
<Project Sdk="MSBuild.Sdk.PgProj/1.0.0">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PreDeploy Include="Pre-Deployment\Script.PreDeployment.sql" />
    <PostDeploy Include="Post-Deployment\Script.PostDeployment.sql" />
  </ItemGroup>
</Project>
```

#### Dependencies

- **Related to:** Issue #16 (SQLCMD Variables), Issue #22 (DacPackage)

#### Labels

`enhancement`, `packaging`, `deployment`, `medium-priority`

---

### Issue #16: Implement SQLCMD Variable Support

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Deployment  
**Story Points:** 5  
**Estimated Time:** 1-2 days  
**Target Version:** v0.4.0

#### Description

Implement SQLCMD-style variable support for parameterizing deployment scripts and SQL files.

#### Acceptance Criteria

- [ ] Support `<SqlCmdVariable>` items in project file
- [ ] Define variables with default values
- [ ] Override variables at deployment time
- [ ] Substitute variables in SQL scripts
- [ ] Support variable syntax: `$(VariableName)`
- [ ] Validate variable references
- [ ] Report undefined variable usage
- [ ] Support in pre/post deployment scripts
- [ ] Support in object definitions (where appropriate)
- [ ] Store variables in `.pgpac` metadata
- [ ] Pass variables to publishing process
- [ ] CLI support for variable values
- [ ] Test with complex variable scenarios

#### Example

```xml
<ItemGroup>
  <SqlCmdVariable Include="TargetEnvironment">
    <DefaultValue>Development</DefaultValue>
  </SqlCmdVariable>
  <SqlCmdVariable Include="DataPath">
    <DefaultValue>/var/lib/postgresql/data</DefaultValue>
  </SqlCmdVariable>
</ItemGroup>
```

```sql
-- Usage in script
CREATE TABLESPACE app_data LOCATION '$(DataPath)/app';
```

#### Dependencies

- **Related to:** Issue #15 (Pre/Post Scripts), Issue #23 (CLI Publish)

#### Labels

`enhancement`, `deployment`, `medium-priority`

---

### Issue #17: Implement Package and Project References

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Packaging, Compilation  
**Story Points:** 13  
**Estimated Time:** 1 week  
**Target Version:** v0.5.0

#### Description

Implement support for referencing other `.pgpac` packages via NuGet packages and project references, similar to MSBuild.Sdk.SqlProj.

#### Acceptance Criteria

##### PackageReference Support
- [ ] Support `<PackageReference>` for NuGet packages containing `.pgpac`
- [ ] Assume `.pgpac` is in `tools/` folder with same name as package
- [ ] Support `DacpacName` attribute for custom naming
- [ ] Support `DatabaseVariableLiteralValue` for different database references
- [ ] Support `DatabaseSqlCmdVariable` and `ServerSqlCmdVariable`
- [ ] Support `SuppressMissingDependenciesErrors` for circular references

##### ProjectReference Support
- [ ] Support `<ProjectReference>` to other `.pgproj` projects
- [ ] Build referenced projects first
- [ ] Include referenced packages in compilation
- [ ] Support same metadata as PackageReference

##### Implementation
- [ ] Include reference metadata in build output
- [ ] Resolve transitive dependencies
- [ ] Handle version conflicts
- [ ] Test with complex dependency graphs
- [ ] Document reference behavior

#### Example

```xml
<ItemGroup>
  <PackageReference Include="MySharedDb" Version="1.0.0" 
                    DatabaseVariableLiteralValue="SharedDatabase" />
  <ProjectReference Include="../CoreDb/CoreDb.pgproj" 
                    DatabaseSqlCmdVariable="CoreDb" />
</ItemGroup>
```

#### Dependencies

- **Blocks:** Issue #18 (System Database References)
- **Related to:** Issue #9 (Compiler Reference Validation)

#### Labels

`enhancement`, `packaging`, `medium-priority`

---

### Issue #18: Reference Master/System Database Packages

**Status:** ?? Not Started  
**Priority:** P2 - Medium  
**Component:** Packaging  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.5.0

#### Description

Create NuGet packages for PostgreSQL system databases (like `postgres`) and support referencing them to avoid warnings when using system objects.

#### Acceptance Criteria

- [ ] Create `.pgpac` for `postgres` database
- [ ] Create `.pgpac` for `template1` database
- [ ] Package system databases as NuGet packages
- [ ] Support versioning by PostgreSQL version (12-16)
- [ ] Document how to reference system databases
- [ ] Test reference resolution
- [ ] Publish packages to NuGet.org (or private feed)
- [ ] Update documentation with examples

#### Example

```xml
<ItemGroup>
  <PackageReference Include="PostgreSQL.SystemDb.Postgres" 
                    Version="16.0.0" 
                    DacpacName="postgres" 
                    DatabaseVariableLiteralValue="postgres" />
</ItemGroup>
```

#### Dependencies

- **Depends on:** Issue #17 (Package References)

#### Labels

`enhancement`, `packaging`, `medium-priority`

---

## Lower Priority Issues

### Issue #19: Implement DacPackage Creation and Loading

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** Packaging  
**Story Points:** 8  
**Estimated Time:** 2-3 days  
**Target Version:** v0.5.0

#### Description

Complete the `DacPackage` class to create and load `.pgpac` files (ZIP format) containing model, scripts, and metadata.

*(Full details available in PROJECT_BOARD.md)*

#### Labels

`enhancement`, `packaging`, `low-priority`

---

### Issue #20: Add NuGet Packaging Support

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** Packaging  
**Story Points:** 5  
**Target Version:** v0.5.0

#### Description

Enable `dotnet pack` to create NuGet packages containing `.pgpac` files for distribution.

#### Labels

`enhancement`, `packaging`, `low-priority`

---

### Issue #21: Create MSBuild SDK Package

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** SDK  
**Story Points:** 21  
**Target Version:** v1.0.0

#### Description

Create an MSBuild SDK package that provides the same project experience as MSBuild.Sdk.SqlProj but for PostgreSQL.

#### Labels

`enhancement`, `sdk`, `low-priority`

---

### Issue #22: Create Project and Item Templates

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** SDK, Templates  
**Story Points:** 8  
**Target Version:** v1.0.0

#### Description

Create `dotnet new` templates for creating new projects and database objects.

#### Labels

`enhancement`, `templates`, `low-priority`

---

### Issue #23: Implement CLI Commands

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** CLI  
**Story Points:** 21  
**Target Version:** v0.4.0

#### Description

Complete the `postgresPacTools` CLI with all essential commands (extract, build, publish, compare, script, report).

#### Labels

`enhancement`, `cli`, `low-priority`

---

### Issue #24: Implement Container Image Publishing

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** Deployment, Container  
**Story Points:** 8  
**Target Version:** v1.0.0

#### Description

Implement container image publishing that includes both the `.pgpac` file and deployment tooling.

#### Labels

`enhancement`, `deployment`, `container`, `low-priority`

---

### Issue #25: Create Comprehensive Documentation

**Status:** ?? Not Started  
**Priority:** P3 - Low  
**Component:** Documentation  
**Story Points:** 21  
**Target Version:** v1.0.0

#### Description

Create comprehensive documentation covering all aspects of the tool including getting started, reference, and best practices.

#### Labels

`documentation`, `low-priority`

---

## Issue Statistics

### By Priority
- **P0 - Critical:** 1 issue (4%)
- **P1 - High (MVP):** 10 issues (40%)
- **P2 - Medium:** 7 issues (28%)
- **P3 - Low:** 7 issues (28%)

### By Component
- **Extraction:** 7 issues
- **Compilation:** 3 issues
- **Comparison:** 3 issues
- **Packaging:** 6 issues
- **Deployment:** 4 issues
- **CLI:** 1 issue
- **SDK:** 2 issues
- **Testing:** 1 issue
- **Documentation:** 1 issue

### By Story Points
- **Total Story Points:** 213
- **MVP Story Points (P0-P1):** 89
- **Average per issue:** 8.5

### Estimated Timeline
- **MVP (v0.1.0 - v0.2.0):** 12-16 weeks
- **Post-MVP (v0.3.0 - v0.5.0):** 12-16 weeks
- **Production Ready (v1.0.0):** 8-12 weeks
- **Total:** 32-44 weeks (8-11 months)

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-31  
**Next Review:** When migrating to GitHub Issues
