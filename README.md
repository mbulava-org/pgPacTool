# pgPacTool - PostgreSQL Data-Tier Application Compiler

A modern .NET 10 tool providing SQL Server Data Tools (.sqlproj) functionality for PostgreSQL databases.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%2B-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Current State
* pgpac file can be generated, with orginal source code and Ast (for comparisions) included, from an existing Database
  * pgdac file is a Simple Json Document and "should" have everything needed to compare/script for the **Supported** object types
  * this should get more and better testing too
* SchemaDiff can be generated, from a "Source" to "Target"
  * Requires testing
## Supported Objects
* Roles (ownership, membership, & privileges)
* Schemas
* Tables
  * columns
  * constraints
  * indexes
* User-Defined Types
  * Enum
  * Domain
  * Composite
* Sequences
## Currently Working on
* Generate Table Migration scripts
  * Must be sure Dependant objects are updated in the correct order
  * Must Create & properly apply Publish Options, simlar to SqlPrij PublishOptions
* Custom MSBuild target to compile validate an entire Project
  * Dependancy Validations
  * Create ONLY scripts to build
    * I think a few ALTERs may need to be allowed for a complete comparision to function
  * Any other Checks?
  * Injection for Code Analyzers?
    
## Testing
* Setup a Postgres 16+ instance using Docker.
* Look for some sample projects https://github.com/neondatabase-labs/postgres-sample-dbs
* Follow the instructions on how to deploy each database.
* UnitTest1.Test1 - Update the Connection string so that it points to your Postgres instance, and database.
* I need to expand the Unit Tests, but this is a start, and other projects are easier to build out than something new...

- **Database Version Control** - Store your database schema as code
- **Schema Comparison** - Compare databases and generate migration scripts
- **Deployment Automation** - Deploy schema changes safely and reliably
- **Package Distribution** - Share databases as NuGet packages
- **MSBuild Integration** - Build databases like any other .NET project

**Inspired by:** [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) but designed specifically for PostgreSQL.

---

## Notes
* pg_query.dll in the mbulava.PostgreSql.Dac project is a Native library that is licensed under the PostgreSQL License, which is a permissive free software license, similar to the MIT License. You can find more details about the PostgreSQL License here: https://www.postgresql.org/about/licence/
* I'm currently working on building multiple versions (16.0+ ONLY) of pg_query for multiple platforms (Windows, Linux, MacOS), and properly including it in the "mbulava-org.Npgquery" package. If you need a specific build or have any questions regarding the usage of pg_query.dll, please feel free to open an issue in the repository or contact me directly.
