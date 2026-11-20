using System.Text;

namespace MessageFormat2.Formatting;

/// <summary>Literal text from the message.</summary>
public sealed class TextPart : MessagePart
{
    internal TextPart(string value)
    {
        Value = value;
    }

    /// <summary>The text.</summary>
    public string Value { get; }

    internal override void AppendTo(StringBuilder builder) => builder.Append(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
