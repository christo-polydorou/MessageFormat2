using System.Text.Json;

namespace MessageFormat2.Tests.Conformance;

// One case from the official test suite, with the file's defaultTestProperties already applied.
// Field names follow spec-tests/schemas/v0/tests.schema.json.
public sealed record ConformanceTestCase
{
    public required string File { get; init; }

    // zero-based position in the file
    public required int Index { get; init; }

    public string? Description { get; init; }

    public required string Locale { get; init; }

    public required string Src { get; init; }

    // "default" or "none"
    public string? BidiIsolation { get; init; }

    public JsonElement? Params { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string? Exp { get; init; }

    public JsonElement? ExpParts { get; init; }

    // Null when the case doesn't say; empty when it expects no errors.
    public IReadOnlyList<string>? ExpErrors { get; init; }

    // shown in test explorers
    public override string ToString() => $"{File} #{Index}: {Description ?? Src}";
}
