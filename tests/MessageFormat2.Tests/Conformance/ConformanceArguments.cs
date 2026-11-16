using System.Globalization;
using System.Text.Json;

namespace MessageFormat2.Tests.Conformance;

public static class ConformanceArguments
{
    // "und" (undetermined) maps to the invariant culture.
    public static CultureInfo Culture(string locale) =>
        locale == "und" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(locale);

    public static Dictionary<string, object?> From(JsonElement? parameters)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (parameters is not { } array)
        {
            return arguments;
        }

        foreach (JsonElement parameter in array.EnumerateArray())
        {
            string name = parameter.GetProperty("name").GetString()!;
            JsonElement value = parameter.GetProperty("value");
            arguments[name] = parameter.TryGetProperty("type", out JsonElement type) && type.GetString() == "datetime"
                ? DateTime.Parse(value.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                : ToValue(value);
        }

        return arguments;
    }

    private static object? ToValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.Clone(),
    };
}
