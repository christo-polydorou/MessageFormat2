using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>A sequence of text and placeholders that makes up the output of a message.</summary>
public sealed class Pattern
{
    /// <summary>An empty pattern.</summary>
    public static Pattern Empty { get; } = new([]);

    /// <summary>Creates a pattern.</summary>
    public Pattern(IEnumerable<PatternElement> elements)
    {
        Elements = ReadOnly.Copy(elements);
    }

    /// <summary>The text and placeholders, in source order.</summary>
    public IReadOnlyList<PatternElement> Elements { get; }
}
