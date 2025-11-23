# pgPacTool
The pgPacTool Library is intended to function almost Identically to the https://github.com/rr-wfm/MSBuild.Sdk.SqlProj repository, except rather than targeting Microsoft SQL Server the Source and Target Database system will be Postgres 16 and above. This is being done with some help of a few existing .Net projects I've found primarily the https://github.com/mbulava-org/Npgquery (my fork) repository which is a wrapper Library of the pg_query which is a Native C library.


## Key Functionality
* Import existing Postgres database into a pgPac file
* Compare Source & Target Database layout and Generate a SchemaDiff
  * Source can be pgPac file, or a Live Database
  * Target can be a pgPac file or a Live Database
  * Options to include/exclude options as needed
* Generate, and optionally execute, a Publish Script, to make the "Source" match the "Target"
  * Options to include/exclude options as needed

## Current State
* pgpac file can be generated, with orginal source code and Ast (for comparisions) included, from an existing Database
 * pgdac file is a Simple Json Document and "should" have everything needed to compare/script for the **Supported** object types
* SchemaDiff can be generated, from a "Source" to "Target"
  * Requires testing
## Supported Objects
* Roles (ownership, membership, & privileges)
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




## Notes
* pg_query.dll in the mbulava.PostgreSql.Dac project is a Native library that is licensed under the PostgreSQL License, which is a permissive free software license, similar to the MIT License. You can find more details about the PostgreSQL License here: https://www.postgresql.org/about/licence/
* I'm currently working on building multiple versions (16.0+ ONLY) of pg_query for multiple platforms (Windows, Linux, MacOS), and properly including it in the "mbulava-org.Npgquery" package. If you need a specific build or have any questions regarding the usage of pg_query.dll, please feel free to open an issue in the repository or contact me directly.
