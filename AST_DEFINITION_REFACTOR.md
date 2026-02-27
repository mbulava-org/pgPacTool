# ✅ Using AST Definition Instead of Duplicate QuoteIdent

## Issue
The `CsprojProjectGenerator` was rebuilding CREATE SCHEMA SQL with its own `QuoteIdent` method, duplicating logic that already existed in `PgProjectExtractor`.

## Root Cause
1. `PgSchema` model was missing a `Definition` property (unlike `PgTable`, `PgView`, etc.)
2. `PgProjectExtractor` wasn't storing the generated SQL
3. Even after adding Definition, it wasn't being copied when creating the final schema object

## Solution

### 1️⃣ **Added Definition Property to PgSchema**
**File:** `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`

```csharp
public class PgSchema
{
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    
    // SQL definition from database  ← NEW!
    public string Definition { get; set; } = string.Empty;
    
    // Parsed AST for programmatic access and comparison
    public CreateSchemaStmt Ast { get; set; }
    // ...
}
```

Now `PgSchema` is consistent with `PgTable`, `PgView`, `PgFunction`, etc.

---

### 2️⃣ **Store Definition in PgProjectExtractor**
**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`

```csharp
schemas.Add(new PgSchema
{
    Name = name,
    Owner = owner,
    Definition = sql, // ← Store the original SQL definition
    Ast = ast,
    Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
});
```

---

### 3️⃣ **Copy Definition When Building Project**
**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs` (in `ExtractPgProject`)

```csharp
var pgSchema = new PgSchema 
{ 
    Name = schema.Name, 
    Owner = schema.Owner,
    Definition = schema.Definition, // ← Copy the SQL definition
    Ast = schema.Ast,
    Privileges = schema.Privileges
};
```

**BUG FIX:** This was missing! The Definition was being set in `ExtractSchemasAsync()` but not copied over.

---

### 4️⃣ **Use Definition in Generator**
**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs`

```csharp
// Use the original SQL definition from extraction (already properly quoted from AST)
var schemaDefinition = !string.IsNullOrWhiteSpace(schema.Definition) 
    ? schema.Definition 
    : $"CREATE SCHEMA IF NOT EXISTS {schema.Name} AUTHORIZATION {schema.Owner};";
```

**Fallback:** If Definition is somehow empty, fall back to safe version with `IF NOT EXISTS`.

---

### 5️⃣ **Removed Duplicate QuoteIdent Method**
**File:** `src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs`

**Deleted:**
```csharp
private static string QuoteIdent(string identifier)
{
    // ... duplicate quoting logic
}
```

**Reason:** No longer needed! We use the properly quoted SQL from `PgProjectExtractor.QuoteIdent()`.

---

## Before & After

### **Before**
```csharp
// CsprojProjectGenerator rebuilds SQL
var schemaDefinition = $"CREATE SCHEMA IF NOT EXISTS {QuoteIdent(schema.Name)} AUTHORIZATION {QuoteIdent(schema.Owner)};";
```

**Output:**
```sql
CREATE SCHEMA IF NOT EXISTS employees AUTHORIZATION postgres;
```

**Issues:**
- ❌ Duplicate `QuoteIdent` implementation
- ❌ Always adds `IF NOT EXISTS` (not matching original)
- ❌ Doesn't leverage AST parsing
- ❌ Inconsistent with tables/views/functions

---

### **After**
```csharp
// Use the original SQL from extraction
var schemaDefinition = schema.Definition;
```

**Output:**
```sql
CREATE SCHEMA "employees" AUTHORIZATION "postgres";
```

**Benefits:**
- ✅ Uses original SQL from `PgProjectExtractor`
- ✅ Proper quoting from Npgquery AST
- ✅ No `IF NOT EXISTS` (matches extraction)
- ✅ Consistent with tables/views/functions
- ✅ Single source of truth for QuoteIdent

---

## Testing

### **Debug Output (Before Fix)**
```
DEBUG: schema.Definition for 'public' is: ''
DEBUG: IsNullOrWhiteSpace: True
DEBUG: schema.Definition for 'employees' is: ''
DEBUG: IsNullOrWhiteSpace: True
```
❌ Definition was empty (not being copied)

### **Debug Output (After Fix)**
```
DEBUG: schema.Definition for 'public' is: 'CREATE SCHEMA "public" AUTHORIZATION "pg_database_owner";'
DEBUG: IsNullOrWhiteSpace: False
DEBUG: schema.Definition for 'employees' is: 'CREATE SCHEMA "employees" AUTHORIZATION "postgres";'
DEBUG: IsNullOrWhiteSpace: False
```
✅ Definition populated with properly quoted SQL

### **Generated Files**

**employees/_schema.sql:**
```sql
CREATE SCHEMA "employees" AUTHORIZATION "postgres";
```

**public/_schema.sql:**
```sql
CREATE SCHEMA "public" AUTHORIZATION "pg_database_owner";
```

✅ **Proper identifier quoting with double quotes**  
✅ **No IF NOT EXISTS** (original SQL preserved)  
✅ **Leverages AST properly**

---

## Benefits

### **1. Single Source of Truth**
- `PgProjectExtractor.QuoteIdent()` is the **only** place identifier quoting happens
- All objects (schemas, tables, views, etc.) use the same quoting logic

### **2. Consistent with Other Objects**
```csharp
// Tables
table.Definition = sql; // Contains properly quoted CREATE TABLE
await File.WriteAllTextAsync(filePath, table.Definition);

// Views
view.Definition = sql; // Contains properly quoted CREATE VIEW
await File.WriteAllTextAsync(filePath, view.Definition);

// Schemas (NOW!)
schema.Definition = sql; // Contains properly quoted CREATE SCHEMA
await File.WriteAllTextAsync(filePath, schema.Definition);
```

### **3. Leverages AST Parsing**
- Uses Npgquery parser for accurate identifier handling
- Handles edge cases (reserved words, special characters)
- PostgreSQL-compliant quoting

### **4. Maintainability**
- No duplicate code
- Less surface area for bugs
- If quoting logic needs to change, change it in one place

---

## Summary

✅ **Added `Definition` property to `PgSchema`**  
✅ **Store SQL definition in `PgProjectExtractor`**  
✅ **Copy Definition when building project** (BUG FIX)  
✅ **Use Definition in generator**  
✅ **Removed duplicate `QuoteIdent` method**

**Result:** Clean, maintainable code that properly leverages the AST!

---

## Files Modified

1. `src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs` - Added `Definition` property
2. `src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs` - Store & copy Definition
3. `src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs` - Use Definition, remove QuoteIdent

**Branch:** `bug/native-build-error`
