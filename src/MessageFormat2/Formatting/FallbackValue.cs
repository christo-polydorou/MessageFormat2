using MessageFormat2.Functions;

namespace MessageFormat2.Formatting;

// What a variable or expression resolves to when it fails. Never passed to functions.
// The fallback text ({$name} and so on) depends on where it is shown, so it's worked out from the
// placeholder's expression at format time, not stored here.
internal sealed class FallbackValue : MessageValue
{
    public static readonly FallbackValue Instance = new();

    private FallbackValue()
    {
    }

    public override string Type => "fallback";

    public override object? Value => null;
}
