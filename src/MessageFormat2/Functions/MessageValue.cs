namespace MessageFormat2.Functions;

/// <summary>
/// The resolved value of an expression: what a function returns, what a variable holds, and
/// what is passed to the next function. Values must be immutable.
/// </summary>
public abstract class MessageValue
{
    /// <summary>Creates a value.</summary>
    protected MessageValue()
    {
    }

    /// <summary>
    /// A short name for the kind of value, such as <c>"string"</c> or <c>"number"</c>.
    /// Reported as <see cref="Formatting.ExpressionPart.Type"/> when formatting to parts.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// The underlying value, for use by other functions: a number value returns the number.
    /// When the value is used as an option, this is what the option holds.
    /// </summary>
    public abstract object? Value { get; }

    /// <summary>The locale the value was formatted for, as a BCP 47 tag, if it has one.</summary>
    public virtual string? Locale => null;

    /// <summary>The direction of the formatted text. Used by bidirectional isolation.</summary>
    public virtual TextDirection Direction => TextDirection.Unknown;

    /// <summary>Formats the value as text.</summary>
    /// <exception cref="MessageFunctionException">The value cannot be formatted. The default implementation always throws.</exception>
    public virtual string FormatToString() =>
        throw new MessageFunctionException(MessageErrorType.UnsupportedOperation, $"A '{Type}' value cannot be formatted.");

    /// <summary>
    /// The pieces of the formatted value, such as the integer and fraction of a number, reported as
    /// <see cref="Formatting.ExpressionPart.Parts"/>. The default is an empty list.
    /// </summary>
    public virtual IReadOnlyList<Formatting.ExpressionSubpart> FormatToParts() => [];

    /// <summary>
    /// Whether the value can be used as a selector after <c>.match</c>.
    /// If true, override <see cref="Matches"/> and, if some keys are better than others, <see cref="IsBetterMatch"/>.
    /// </summary>
    public virtual bool SupportsSelection => false;

    /// <summary>Whether a variant key matches this value.</summary>
    /// <param name="key">The key, in Unicode Normalization Form C. Never the catch-all <c>*</c>.</param>
    /// <exception cref="MessageFunctionException">
    /// With <see cref="MessageErrorType.BadVariantKey"/>: this key is not valid for the value; it
    /// does not match, and selection carries on. With any other type: selection failed, and the
    /// selector then matches only <c>*</c>.
    /// </exception>
    public virtual bool Matches(string key) => false;

    /// <summary>
    /// Whether <paramref name="key1"/> is a better match than <paramref name="key2"/>; only called
    /// with two keys that both match. For the number 1, the key <c>1</c> is better than <c>one</c>.
    /// </summary>
    public virtual bool IsBetterMatch(string key1, string key2) => false;
}
