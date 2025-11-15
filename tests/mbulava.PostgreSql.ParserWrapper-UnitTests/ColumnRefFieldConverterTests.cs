using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.ParserWrapper_UnitTests
{
    using Xunit;
    using System.Text.Json;

    public class ColumnRefFieldConverterTests
    {
        [Fact]
        public void Deserialize_StringField_ReturnsString()
        {
            string json = "\"id\"";
            var result = JsonSerializer.Deserialize<object>(json, new JsonSerializerOptions
            {
                Converters = { new ColumnRefFieldConverter() }
            });

            Assert.IsType<string>(result);
            Assert.Equal("id", result);
        }

        [Fact]
        public void Deserialize_AStarField_ReturnsAStar()
        {
            string json = "{\"A_Star\":{}}";
            var result = JsonSerializer.Deserialize<object>(json, new JsonSerializerOptions
            {
                Converters = { new ColumnRefFieldConverter() }
            });

            Assert.IsType<AStar>(result);
        }

        [Fact]
        public void Deserialize_InvalidField_Throws()
        {
            string json = "{\"Unknown\":{}}";
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<object>(json, new JsonSerializerOptions
                {
                    Converters = { new ColumnRefFieldConverter() }
                }));
        }
    }
}
