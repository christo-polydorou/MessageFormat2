using System.Text;

namespace MessageFormat2.Formatting;

/// <summary>A formatted placeholder, such as a number or a string.</summary>
public sealed class ExpressionPart : MessagePart
{
    internal ExpressionPart(string type, string value, IReadOnlyList<ExpressionSubpart> parts, string? locale, TextDirection direction, string? id, bool isolate)
    {
        Type = type;
        Value = value;
        Parts = parts;
        Locale = locale;
        Direction = direction;
        Id = id;
        Isolate = isolate;
    }

    /// <summary>The kind of value, such as <c>"string"</c> or <c>"number"</c>.</summary>
    public string Type { get; }

    /// <summary>The formatted text.</summary>
    public string Value { get; }

    /// <summary>The pieces of the formatted text, such as the integer and fraction of a number. May be empty.</summary>
    public IReadOnlyList<ExpressionSubpart> Parts { get; }

    /// <summary>The locale the value was formatted for, as a BCP 47 tag, if known.</summary>
    public string? Locale { get; }

    /// <summary>The direction of the formatted text.</summary>
    public TextDirection Direction { get; }

    /// <summary>The value of the placeholder's <c>u:id</c> option, if set.</summary>
    public string? Id { get; }

    internal bool Isolate { get; }

    internal override void AppendTo(StringBuilder builder) => builder.Append(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
