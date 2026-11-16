using System.Text.Json;

namespace MessageFormat2.Tests.Conformance;

public static class ConformanceSuite
{
    private static readonly string TestsDirectory = Path.Combine(AppContext.BaseDirectory, "spec-tests", "tests");

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<ConformanceTestCase>>> Files = new(LoadAll);

    public static IEnumerable<string> FileNames => Files.Value.Keys.Order(StringComparer.Ordinal);

    public static IReadOnlyList<ConformanceTestCase> Cases(string file) => Files.Value[file];

    private static Dictionary<string, IReadOnlyList<ConformanceTestCase>> LoadAll()
    {
        return Directory
            .EnumerateFiles(TestsDirectory, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(TestsDirectory, path).Replace('\\', '/'))
            .ToDictionary(file => file, file => (IReadOnlyList<ConformanceTestCase>)Load(file), StringComparer.Ordinal);
    }

    private static List<ConformanceTestCase> Load(string file)
    {
        using JsonDocument document = JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(TestsDirectory, file)));
        JsonElement root = document.RootElement;

        JsonElement defaults = root.TryGetProperty("defaultTestProperties", out JsonElement d) ? d : default;

        return root.GetProperty("tests")
            .EnumerateArray()
            .Select((test, index) => Create(file, index, test, defaults))
            .ToList();
    }

    private static ConformanceTestCase Create(string file, int index, JsonElement test, JsonElement defaults)
    {
        // A property on the test overrides the same property in defaultTestProperties.
        JsonElement? Get(string name)
        {
            if (test.TryGetProperty(name, out JsonElement value))
            {
                return value.Clone();
            }

            if (defaults.ValueKind == JsonValueKind.Object && defaults.TryGetProperty(name, out value))
            {
                return value.Clone();
            }

            return null;
        }

        return new ConformanceTestCase
        {
            File = file,
            Index = index,
            Description = Get("description")?.GetString(),
            Locale = Get("locale")?.GetString() ?? throw new InvalidDataException($"{file} #{index} has no locale."),
            Src = Get("src")?.GetString() ?? throw new InvalidDataException($"{file} #{index} has no src."),
            BidiIsolation = Get("bidiIsolation")?.GetString(),
            Params = Get("params"),
            Tags = Get("tags")?.EnumerateArray().Select(tag => tag.GetString()!).ToList() ?? [],
            Exp = Get("exp")?.GetString(),
            ExpParts = Get("expParts"),
            ExpErrors = Get("expErrors")?.EnumerateArray().Select(error => error.GetProperty("type").GetString()!).ToList(),
        };
    }
}
