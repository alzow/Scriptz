using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Operator.Models;

// PostgREST serializes Postgres `time` columns as "HH:mm:ss" strings; MAUI's TimePicker binds to TimeSpan.
public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return TimeSpan.TryParse(str, out var result) ? result : TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
    }
}
