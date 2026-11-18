namespace MessageFormat2.DataModel;

/// <summary>Literal text in a pattern, with escapes already processed.</summary>
public sealed class Text : PatternElement
{
    /// <summary>Creates a text element.</summary>
    /// <param name="value">The text. Must not be empty.</param>
    public Text(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    /// <summary>The text, with escapes already processed.</summary>
    public string Value { get; }
}
