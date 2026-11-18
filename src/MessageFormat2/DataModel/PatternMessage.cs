namespace MessageFormat2.DataModel;

/// <summary>A message with a single pattern and no selectors.</summary>
/// <example><c>Hello, {$name}!</c></example>
public sealed class PatternMessage : Message
{
    /// <summary>Creates a pattern message.</summary>
    public PatternMessage(IEnumerable<Declaration> declarations, Pattern pattern)
        : base(declarations)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Pattern = pattern;
    }

    /// <summary>The pattern to format.</summary>
    public Pattern Pattern { get; }
}
