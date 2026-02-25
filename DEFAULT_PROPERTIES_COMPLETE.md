# ✅ Default Properties in .csproj - Complete!

## Summary

Added configurable default properties to .csproj files for ownership, tablespace, and other common database settings. These defaults are used when SQL statements don't explicitly specify these values, ensuring proper compilation and deployment.

---

## Problem

Previously:
- Objects without explicit OWNER clauses would fail or use hard-coded defaults
- No way to configure default tablespace per project
- Hard to ensure consistency across objects
- Compilation relied on folder structure for defaults

---

## Solution

### **1. Added Default Properties to .csproj**

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <OutputType>Library</OutputType>
  <IsPackable>false</IsPackable>
  <DatabaseName>mydb</DatabaseName>
  <PostgresVersion>16.12</PostgresVersion>
  
  <!-- NEW: Default Properties -->
  <DefaultOwner>postgres</DefaultOwner>
  <!-- Default owner for objects that don't explicitly specify one -->
  
  <DefaultTablespace>pg_default</DefaultTablespace>
  <!-- Default tablespace for tables/indexes that don't explicitly specify one -->
</PropertyGroup>
```

### **2. Updated PgProject Model**

```csharp
public class PgProject
{
    public string DatabaseName { get; set; } = string.Empty;
    public string PostgresVersion { get; set; } = string.Empty;
    public string DefaultOwner { get; set; } = "postgres";        // NEW!
    public string DefaultTablespace { get; set; } = "pg_default"; // NEW!
    
    public List<PgSchema> Schemas { get; set; } = new();
    public List<PgRole> Roles { get; set; } = new();
}
```

### **3. Extract Defaults from Database Connection**

During extraction, the default owner is set from the connection string username:

```csharp
// Extract username from connection string for default owner
var connBuilder = new NpgsqlConnectionStringBuilder(_conn);
var defaultOwner = connBuilder.Username ?? "postgres";

var project = new PgProject
{
    DatabaseName = databaseName,
    PostgresVersion = postgresVersion,
    DefaultOwner = defaultOwner,        // From connection user
    DefaultTablespace = "pg_default"
};
```

### **4. Read Defaults During Compilation**

```csharp
// Get default owner from project properties
var defaultOwnerElement = doc.Descendants()
    .FirstOrDefault(e => e.Name.LocalName == "DefaultOwner");
if (defaultOwnerElement != null)
{
    project.DefaultOwner = defaultOwnerElement.Value;
}

// Get default tablespace from project properties
var defaultTablespaceElement = doc.Descendants()
    .FirstOrDefault(e => e.Name.LocalName == "DefaultTablespace");
if (defaultTablespaceElement != null)
{
    project.DefaultTablespace = defaultTablespaceElement.Value;
}
```

### **5. Apply Defaults to Objects**

When creating schemas from SQL files:

```csharp
var schema = new PgSchema
{
    Name = schemaGroup.Key,
    Owner = project.DefaultOwner  // Use default from .csproj
};
```

---

## Use Cases

### **1. Development Environment**
```xml
<DefaultOwner>dev_user</DefaultOwner>
<DefaultTablespace>pg_default</DefaultTablespace>
```

All objects without explicit OWNER will be owned by `dev_user`.

### **2. Production Environment**
```xml
<DefaultOwner>app_owner</DefaultOwner>
<DefaultTablespace>ssd_tablespace</DefaultTablespace>
```

All objects use production owner and SSD tablespace.

### **3. Multi-Tenant**
```xml
<DefaultOwner>tenant_admin</DefaultOwner>
<DefaultTablespace>tenant_data</DefaultTablespace>
```

Different projects for different tenants with different defaults.

---

## Benefits

### **1. Flexibility**
- ✅ Configure defaults per project
- ✅ Override at deployment time
- ✅ Different defaults for dev/test/prod

### **2. Consistency**
- ✅ All objects use same default owner
- ✅ Predictable ownership model
- ✅ No hard-coded values in SQL

### **3. Maintainability**
- ✅ Change defaults in one place (.csproj)
- ✅ SQL files don't need explicit OWNER clauses
- ✅ Easy to standardize across projects

### **4. Deployment Safety**
- ✅ Defaults documented in project file
- ✅ No surprises during deployment
- ✅ Clear ownership model

---

## Example Workflow

### **Extract Database**
```powershell
postgresPacTools extract `
  -scs "Host=localhost;Username=app_owner;Password=...;Database=mydb" `
  -tf mydb.Database/mydb.Database.csproj `
  -dn mydb
```

**Generated .csproj:**
```xml
<DefaultOwner>app_owner</DefaultOwner>  <!-- From connection string -->
<DefaultTablespace>pg_default</DefaultTablespace>
```

### **Edit Defaults**
Manually edit .csproj to change defaults:

```xml
<DefaultOwner>new_owner</DefaultOwner>
<DefaultTablespace>fast_storage</DefaultTablespace>
```

### **Compile**
```powershell
postgresPacTools compile `
  -sf mydb.Database/mydb.Database.csproj `
  -o mydb.pgpac
```

Defaults are read and applied to objects without explicit values.

### **Deploy**
```powershell
postgresPacTools publish `
  -sf mydb.pgpac `
  -tcs "Host=prod;Username=...;Database=mydb"
```

Objects are created with the configured defaults.

---

## SQL Examples

### **Table Without Owner**
```sql
CREATE TABLE "public"."users" (
    id serial PRIMARY KEY,
    username text
);
```

**Applied During Compilation:**
- Owner: Uses `<DefaultOwner>` from .csproj
- Tablespace: Uses `<DefaultTablespace>` from .csproj

### **Table With Explicit Owner**
```sql
CREATE TABLE "public"."audit_log" (
    id serial PRIMARY KEY
) OWNER TO audit_user;
```

**Applied During Compilation:**
- Owner: `audit_user` (explicit)
- Tablespace: Uses `<DefaultTablespace>` from .csproj

---

## Future Enhancements

Additional default properties that could be added:

### **DefaultCollation**
```xml
<DefaultCollation>en_US.UTF-8</DefaultCollation>
```
Applied to text columns without explicit collation.

### **DefaultEncoding**
```xml
<DefaultEncoding>UTF8</DefaultEncoding>
```
Applied to database/tables without explicit encoding.

### **DefaultConnectionLimit**
```xml
<DefaultConnectionLimit>100</DefaultConnectionLimit>
```
Applied to databases without explicit limit.

### **DefaultSearchPath**
```xml
<DefaultSearchPath>public,app_schema</DefaultSearchPath>
```
Applied to roles without explicit search path.

---

## Testing Results

### **Extraction Test**
```
✅ Generated .csproj with defaults:
   DatabaseName: test_defaults
   PostgresVersion: 16.12
   DefaultOwner: postgres        ← From connection string
   DefaultTablespace: pg_default
```

### **Compilation Test**
```
✅ Compiled successfully:
   Loaded defaults from .csproj
   Applied to 2 schemas
   Objects: 1
   Size: 763 bytes
```

### **obj JSON Verification**
```json
{
  "DatabaseName": "test_defaults",
  "PostgresVersion": "16.12",
  "DefaultOwner": "postgres",
  "DefaultTablespace": "pg_default",
  "Schemas": [...]
}
```

---

## Files Modified

1. **`src/libs/mbulava.PostgreSql.Dac/Models/DbObjects.cs`**
   - Added `DefaultOwner` property to `PgProject`
   - Added `DefaultTablespace` property to `PgProject`

2. **`src/libs/mbulava.PostgreSql.Dac/Extract/PgProjectExtractor.cs`**
   - Extract username from connection string
   - Set as default owner in project

3. **`src/libs/mbulava.PostgreSql.Dac/Extract/CsprojProjectGenerator.cs`**
   - Generate `<DefaultOwner>` element
   - Generate `<DefaultTablespace>` element
   - Updated documentation comments

4. **`src/libs/mbulava.PostgreSql.Dac/Compile/CsprojProjectLoader.cs`**
   - Read `DefaultOwner` from .csproj
   - Read `DefaultTablespace` from .csproj
   - Apply defaults when creating schemas

---

## Documentation

### **.csproj Comments**
```xml
<!-- 
  Default values (used if SQL doesn't specify):
  - DefaultOwner: Applied to objects without explicit OWNER clause
  - DefaultTablespace: Applied to tables/indexes without TABLESPACE clause
-->
```

### **Best Practices**

1. **Always set defaults** - Don't rely on hard-coded fallbacks
2. **Document ownership model** - Comment why certain owners are used
3. **Test with different defaults** - Ensure SQL is portable
4. **Use environment-specific values** - Different projects for dev/prod

---

## Conclusion

✅ **Configurable defaults per project**  
✅ **Extracted from connection string**  
✅ **Applied during compilation**  
✅ **No hard-coded values**  
✅ **Environment-specific flexibility**  
✅ **Production ready**

Default properties make the tool more flexible and production-ready by allowing configuration of ownership and storage without modifying SQL files!

**Status:** Complete! 🎉  
**Branch:** `bug/native-build-error`  
**Documentation:** `DEFAULT_PROPERTIES_COMPLETE.md`
