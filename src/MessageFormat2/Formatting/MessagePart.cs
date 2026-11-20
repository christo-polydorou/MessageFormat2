using System.Text;

namespace MessageFormat2.Formatting;

/// <summary>
/// One piece of a formatted message: <see cref="TextPart"/>, <see cref="ExpressionPart"/>,
/// <see cref="MarkupPart"/>, <see cref="FallbackPart"/>, or <see cref="BidiIsolationPart"/>.
/// Concatenating the parts' text gives the same result as
/// <see cref="MessageFormatter.Format(IReadOnlyDictionary{string, object?})"/>.
/// </summary>
public abstract class MessagePart
{
    private protected MessagePart()
    {
    }

    internal abstract void AppendTo(StringBuilder builder);
}
