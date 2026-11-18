using System.Diagnostics.CodeAnalysis;

namespace MessageFormat2.DataModel;

/// <summary>
/// An attribute on an expression or markup, such as <c>@translate=no</c>.
/// Attributes are for tools and translators and have no effect on formatting.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This is a MessageFormat attribute, not a .NET attribute.")]
public sealed class MessageAttribute
{
    /// <summary>Creates an attribute.</summary>
    /// <param name="name">The attribute identifier, without the leading <c>@</c>.</param>
    /// <param name="value">The attribute's value, or null if it has none.</param>
    public MessageAttribute(string name, Literal? value = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        Value = value;
    }

    /// <summary>The attribute identifier, without the leading <c>@</c>.</summary>
    public string Name { get; }

    /// <summary>The attribute's value, or null if it was written without one.</summary>
    public Literal? Value { get; }
}
