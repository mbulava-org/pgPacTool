namespace mbulava.PostgreSql.ParserWrapper_UnitTests
{
    using Xunit;

    public class PgQueryTests
    {
        [Fact]
        public void Parse_ValidSql_ReturnsJson()
        {
            string sql = "SELECT * FROM users;";
            string json = PgQuery.ParseToJson(sql);

            Assert.Contains("SelectStmt", json);
        }

        [Fact]
        public void Parse_InvalidSql_Throws()
        {
            string sql = "SELECT FROM;";
            Assert.Throws<InvalidOperationException>(() => PgQuery.ParseToJson(sql));
        }
    }
}
