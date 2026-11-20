using MessageFormat2.Functions;

namespace MessageFormat2;

/// <summary>Options for a <see cref="MessageFormatter"/>.</summary>
public sealed class MessageFormatterOptions
{
    /// <summary>
    /// Custom functions, keyed by name without the leading <c>:</c> (for example <c>"my:money"</c>).
    /// They are added to the built-ins and replace one with the same name; the <c>u</c> namespace is reserved.
    /// </summary>
    public IDictionary<string, MessageFunction> Functions { get; } = new Dictionary<string, MessageFunction>(StringComparer.Ordinal);

    /// <summary>How placeholders are isolated in mixed-direction text. The default is <see cref="BidiIsolation.Default"/>.</summary>
    public BidiIsolation BidiIsolation { get; set; } = BidiIsolation.Default;
}
