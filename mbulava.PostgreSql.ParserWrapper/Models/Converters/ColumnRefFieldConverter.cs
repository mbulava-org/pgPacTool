using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.ParserWrapper.Models.Converters
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ColumnRefFieldConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String)
                return root.GetString();

            if (root.TryGetProperty("A_Star", out _))
                return new AStar(); // Custom class for "*"

            throw new JsonException("Unknown ColumnRef field type.");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is string s)
                writer.WriteStringValue(s);
            else if (value is AStar)
            {
                writer.WriteStartObject();
                writer.WriteStartObject("A_Star");
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            else
                throw new JsonException("Unknown ColumnRef field type.");
        }
    }

    public class AStar { }
}
