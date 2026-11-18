using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>
/// A markup placeholder such as <c>{#link href=$url}</c>, <c>{/link}</c>, or <c>{#br/}</c>.
/// Markup produces no text when a message is formatted to a string.
/// </summary>
public sealed class Markup : PatternElement
{
    /// <summary>Creates a markup element.</summary>
    public Markup(
        MarkupKind kind,
        string name,
        IEnumerable<MessageOption>? options = null,
        IEnumerable<MessageAttribute>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Kind = kind;
        Name = name;
        Options = ReadOnly.Copy(options ?? []);
        Attributes = ReadOnly.Copy(attributes ?? []);
    }

    /// <summary>Whether this markup opens, closes, or stands alone.</summary>
    public MarkupKind Kind { get; }

    /// <summary>The markup identifier, without the <c>#</c> or <c>/</c> sigils.</summary>
    public string Name { get; }

    /// <summary>The options, in source order.</summary>
    public IReadOnlyList<MessageOption> Options { get; }

    /// <summary>The attributes, in source order.</summary>
    public IReadOnlyList<MessageAttribute> Attributes { get; }
}
