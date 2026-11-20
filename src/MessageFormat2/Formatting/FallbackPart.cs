using System.Text;

namespace MessageFormat2.Formatting;

/// <summary>
/// A placeholder that could not be formatted. In string output it appears in braces,
/// such as <c>{$name}</c> or <c>{:my:fn}</c>.
/// </summary>
public sealed class FallbackPart : MessagePart
{
    internal FallbackPart(string source)
    {
        Source = source;
    }

    /// <summary>A short form of the placeholder's source, such as <c>$name</c>, <c>|42|</c>, or <c>:my:fn</c>.</summary>
    public string Source { get; }

    internal override void AppendTo(StringBuilder builder) => builder.Append('{').Append(Source).Append('}');

    /// <inheritdoc/>
    public override string ToString() => $"{{{Source}}}";
}
