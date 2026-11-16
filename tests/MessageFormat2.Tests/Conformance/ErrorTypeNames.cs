namespace MessageFormat2.Tests.Conformance;

public static class ErrorTypeNames
{
    public static string ToTestName(MessageErrorType type)
    {
        string name = type.ToString();
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                result.Append('-');
            }

            result.Append(char.ToLowerInvariant(name[i]));
        }

        return result.ToString();
    }

    public static bool IsDataModelError(string testName) => testName is
        "variant-key-mismatch" or
        "missing-fallback-variant" or
        "missing-selector-annotation" or
        "duplicate-declaration" or
        "duplicate-option-name" or
        "duplicate-variant";
}
