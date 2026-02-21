# Npgquery - PostgreSQL Query Parser for .NET

A high-performance .NET library for parsing PostgreSQL queries using the official PostgreSQL parser via libpg_query.



## Key Features

- **Parse SQL to AST**: Convert PostgreSQL queries to JSON or Protobuf AST
- **Query Normalization**: Standardize queries for comparison
- **Query Fingerprinting**: Generate unique identifiers for query patterns
- **Statement Splitting**: Split multi-statement SQL into individual statements
- **Query Tokenization**: Analyze SQL tokens and keywords
- **PL/pgSQL Support**: Parse PL/pgSQL functions and procedures
- **Async Operations**: Full async/await support
- **Batch Processing**: Process multiple queries efficiently
- **Cross-Platform**: Works on Windows, Linux, and macOS

## Documentation

Visit the [GitHub repository](https://github.com/yourusername/Npgquery) for complete documentation, examples, and API reference.

## License

MIT License - see LICENSE file for details.

This library is built on [libpg_query](https://github.com/pganalyze/libpg_query), which embeds the official PostgreSQL parser.