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