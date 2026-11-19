using System.Text;

namespace MessageFormat2.Functions;

/// <summary>A text value: the result of a literal, or of the <c>:string</c> function.</summary>
/// <remarks>Selects by exact match after Unicode Normalization Form C, as <c>:string</c> specifies.</remarks>
public sealed class StringValue : MessageValue
{
    /// <summary>Creates a string value.</summary>
    /// <param name="value">The text.</param>
    /// <param name="locale">The locale, as a BCP 47 tag, if known.</param>
    /// <param name="direction">The direction of the text, if known.</param>
    public StringValue(string value, string? locale = null, TextDirection direction = TextDirection.Unknown)
    {
        ArgumentNullException.ThrowIfNull(value);
        Text = value;
        Locale = locale;
        Direction = direction;
    }

    /// <summary>The text.</summary>
    public string Text { get; }

    /// <inheritdoc/>
    public override string Type => "string";

    /// <inheritdoc/>
    public override object? Value => Text;

    /// <inheritdoc/>
    public override string? Locale { get; }

    /// <inheritdoc/>
    public override TextDirection Direction { get; }

    /// <inheritdoc/>
    public override string FormatToString() => Text;

    /// <inheritdoc/>
    public override bool SupportsSelection => true;

    /// <inheritdoc/>
    public override bool Matches(string key) =>
        string.Equals(Text.Normalize(NormalizationForm.FormC), key, StringComparison.Ordinal);
}
