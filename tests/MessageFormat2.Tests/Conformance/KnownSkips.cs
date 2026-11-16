namespace MessageFormat2.Tests.Conformance;

public static class KnownSkips
{
    // Each entry also records the case's source, so that if the test files are updated and the
    // index now points at a different case, the entry fails instead of silently skipping it.
    private static readonly Dictionary<(string File, int Index), (string Src, string Reason)> Skips = new()
    {
        [("functions/string.json", 3)] = (
            ".input {$foo :string} .match $foo 1 {{one}} * {{other}}",
            "Conflicts with the spec. With $foo missing, the test expects only unresolved-variable, but the spec " +
            "requires a Bad Selector error when a selector fails to resolve (formatting.md, \"Error Handling\"). " +
            "pattern-selection.json and fallback.json expect the matching bad-operand and bad-selector errors."),
    };

    public static string? Reason(ConformanceTestCase testCase)
    {
        if (!Skips.TryGetValue((testCase.File, testCase.Index), out (string Src, string Reason) skip))
        {
            return null;
        }

        if (skip.Src != testCase.Src)
        {
            throw new InvalidOperationException(
                $"The skip entry for {testCase.File} #{testCase.Index} no longer matches the test file. Update KnownSkips.");
        }

        return skip.Reason;
    }

    // For cases that need a feature that isn't built yet. Nothing is pending right now, so this
    // returns null; it stays for future suite updates.
    public static string? PendingFeature(ConformanceTestCase testCase)
    {
        ArgumentNullException.ThrowIfNull(testCase);
        return null;
    }
}
