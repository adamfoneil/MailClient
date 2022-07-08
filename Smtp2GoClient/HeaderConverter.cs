using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smtp2Go
{
    internal class HeaderConverter : JsonConverter<Dictionary<string, string>>
    {
        public override Dictionary<string, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            if (value?.Any() ?? false)
            {                
                foreach (var kp in value)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("header");
                    writer.WriteStringValue(kp.Key);
                    writer.WritePropertyName("value");
                    writer.WriteStringValue(kp.Value);
                    writer.WriteEndObject();
                }                
            }

            writer.WriteEndArray();
        }
    }
}