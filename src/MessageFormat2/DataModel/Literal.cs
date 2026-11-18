namespace MessageFormat2.DataModel;

/// <summary>
/// A literal value, quoted (<c>|hello world|</c>) or unquoted (<c>42</c>).
/// The data model does not record which form was used.
/// </summary>
public sealed class Literal : Operand
{
    /// <summary>Creates a literal.</summary>
    /// <param name="value">The literal's string value, with quotes removed and escapes processed.</param>
    public Literal(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>The literal's string value, with quotes removed and escapes processed.</summary>
    public string Value { get; }
}
