using System.Text;

using MessageFormat2.DataModel;

namespace MessageFormat2.Formatting;

/// <summary>
/// A markup placeholder such as <c>{#link href=$url}</c>. Markup produces no text in string
/// output; it is up to the caller to render it.
/// </summary>
public sealed class MarkupPart : MessagePart
{
    internal MarkupPart(MarkupKind kind, string name, IReadOnlyDictionary<string, object?> options, string? id)
    {
        Kind = kind;
        Name = name;
        Options = options;
        Id = id;
    }

    /// <summary>Whether this markup opens, closes, or stands alone.</summary>
    public MarkupKind Kind { get; }

    /// <summary>The markup identifier, without the <c>#</c> or <c>/</c> sigils.</summary>
    public string Name { get; }

    /// <summary>
    /// The resolved options, as underlying values (<see cref="Functions.MessageValue.Value"/>).
    /// <c>u:id</c> and <c>u:dir</c> are not included.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Options { get; }

    /// <summary>The value of the markup's <c>u:id</c> option, if set.</summary>
    public string? Id { get; }

    internal override void AppendTo(StringBuilder builder)
    {
    }

    /// <inheritdoc/>
    public override string ToString() => Kind switch
    {
        MarkupKind.Open => $"{{#{Name}}}",
        MarkupKind.Standalone => $"{{#{Name}/}}",
        _ => $"{{/{Name}}}",
    };
}
