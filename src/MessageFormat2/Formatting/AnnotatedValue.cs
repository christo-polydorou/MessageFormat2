using System.Diagnostics.CodeAnalysis;

using MessageFormat2.Functions;

namespace MessageFormat2.Formatting;

// A function's result plus the u: options of its expression. u:dir has to stay with the resolved
// value, so that `.local $x = {a :string u:dir=rtl}` followed by `{$x}` is still isolated as rtl.
// Functions never see this wrapper: operands and option values go through Unwrap first.
internal sealed class AnnotatedValue : MessageValue
{
    private readonly UnicodeOptions _options;

    public AnnotatedValue(MessageValue inner, UnicodeOptions options)
    {
        Inner = inner;
        _options = options;
    }

    public MessageValue Inner { get; }

    public string? Id => _options.Id;

    public bool Isolate => _options.Isolate;

    public override string Type => Inner.Type;

    public override object? Value => Inner.Value;

    public override string? Locale => Inner.Locale;

    public override TextDirection Direction => _options.Direction switch
    {
        DirectionOption.LeftToRight => TextDirection.LeftToRight,
        DirectionOption.RightToLeft => TextDirection.RightToLeft,
        DirectionOption.Auto => TextDirection.Unknown,
        _ => Inner.Direction,
    };

    public override string FormatToString() => Inner.FormatToString();

    public override IReadOnlyList<ExpressionSubpart> FormatToParts() => Inner.FormatToParts();

    public override bool SupportsSelection => Inner.SupportsSelection;

    public override bool Matches(string key) => Inner.Matches(key);

    public override bool IsBetterMatch(string key1, string key2) => Inner.IsBetterMatch(key1, key2);

    [return: NotNullIfNotNull(nameof(value))]
    public static MessageValue? Unwrap(MessageValue? value) => value is AnnotatedValue annotated ? annotated.Inner : value;
}
