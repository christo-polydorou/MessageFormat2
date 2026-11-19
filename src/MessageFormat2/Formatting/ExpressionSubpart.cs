namespace MessageFormat2.Formatting;

/// <summary>
/// A piece of a formatted value, such as the <c>integer</c>, <c>group</c>, <c>decimal</c>, or
/// <c>fraction</c> of a number. Part types follow JavaScript's <c>Intl.NumberFormat</c>.
/// </summary>
public sealed class ExpressionSubpart
{
    /// <summary>Creates a subpart.</summary>
    /// <param name="type">The kind of piece, such as <c>"integer"</c>.</param>
    /// <param name="value">The text of the piece.</param>
    public ExpressionSubpart(string type, string value)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);
        Type = type;
        Value = value;
    }

    /// <summary>The kind of piece, such as <c>"integer"</c> or <c>"decimal"</c>.</summary>
    public string Type { get; }

    /// <summary>The text of the piece.</summary>
    public string Value { get; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
