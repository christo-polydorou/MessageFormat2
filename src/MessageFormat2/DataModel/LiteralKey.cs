namespace MessageFormat2.DataModel;

/// <summary>A variant key written as a literal, such as <c>one</c> or <c>|1.0|</c>.</summary>
public sealed class LiteralKey : VariantKey
{
    /// <summary>Creates a literal key.</summary>
    /// <param name="value">The key's string value, with quotes removed and escapes processed.</param>
    public LiteralKey(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>The key's string value, with quotes removed and escapes processed.</summary>
    public string Value { get; }
}
