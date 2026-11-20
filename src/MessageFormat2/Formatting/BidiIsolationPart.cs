using System.Text;

namespace MessageFormat2.Formatting;

/// <summary>A Unicode isolate character (U+2066 to U+2069) added around a placeholder by bidi isolation.</summary>
public sealed class BidiIsolationPart : MessagePart
{
    internal static readonly BidiIsolationPart LeftToRightIsolate = new('⁦');
    internal static readonly BidiIsolationPart RightToLeftIsolate = new('⁧');
    internal static readonly BidiIsolationPart FirstStrongIsolate = new('⁨');
    internal static readonly BidiIsolationPart PopDirectionalIsolate = new('⁩');

    private BidiIsolationPart(char value)
    {
        Value = value.ToString();
    }

    /// <summary>The isolate character, as a one-character string.</summary>
    public string Value { get; }

    internal override void AppendTo(StringBuilder builder) => builder.Append(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
