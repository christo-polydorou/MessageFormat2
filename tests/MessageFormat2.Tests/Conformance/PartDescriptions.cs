using System.Text.Json;
using System.Text.Json.Nodes;

using MessageFormat2.DataModel;
using MessageFormat2.Formatting;

namespace MessageFormat2.Tests.Conformance;

// Converts formatted parts to the JSON shape used by expParts in the test suite, and compares them
// the way the reference implementation does.
public static class PartDescriptions
{
    public static JsonObject Describe(MessagePart part) => part switch
    {
        TextPart text => new() { ["type"] = "text", ["value"] = text.Value },
        BidiIsolationPart bidi => new() { ["type"] = "bidiIsolation", ["value"] = bidi.Value },
        FallbackPart fallback => new() { ["type"] = "fallback", ["source"] = fallback.Source },
        MarkupPart markup => Without(new JsonObject
        {
            ["type"] = "markup",
            ["kind"] = markup.Kind.ToString().ToLowerInvariant(),
            ["name"] = markup.Name,
            ["id"] = markup.Id,
            ["options"] = new JsonObject(markup.Options.Select(option =>
                KeyValuePair.Create(option.Key, (JsonNode?)JsonValue.Create(option.Value?.ToString())))),
        }),
        ExpressionPart expression => Without(new JsonObject
        {
            ["type"] = expression.Type,
            ["value"] = expression.Value,
            ["locale"] = expression.Locale,
            ["id"] = expression.Id,
            ["dir"] = expression.Direction switch
            {
                TextDirection.LeftToRight => "ltr",
                TextDirection.RightToLeft => "rtl",
                _ => null,
            },
            ["parts"] = expression.Parts.Count == 0
                ? null
                : new JsonArray(expression.Parts.Select(part => (JsonNode)new JsonObject { ["type"] = part.Type, ["value"] = part.Value }).ToArray()),
        }),
        _ => throw new NotSupportedException($"Unknown part type {part.GetType()}."),
    };

    // Extra properties in actual are allowed.
    public static bool Contains(JsonNode? actual, JsonElement expected) => expected.ValueKind switch
    {
        JsonValueKind.Object => actual is JsonObject actualObject && expected.EnumerateObject().All(
            property => Contains(actualObject[property.Name], property.Value)),
        JsonValueKind.Array => actual is JsonArray actualArray
            && actualArray.Count == expected.GetArrayLength()
            && expected.EnumerateArray().Select((item, index) => Contains(actualArray[index], item)).All(match => match),
        JsonValueKind.Null => actual is null,
        _ => actual is not null && actual.ToJsonString() == expected.GetRawText(),
    };

    private static JsonObject Without(JsonObject node)
    {
        foreach (string key in node.Where(property => property.Value is null).Select(property => property.Key).ToList())
        {
            node.Remove(key);
        }

        return node;
    }
}
