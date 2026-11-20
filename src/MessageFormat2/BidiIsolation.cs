namespace MessageFormat2;

/// <summary>How placeholders are isolated from the surrounding text in mixed-direction output.</summary>
public enum BidiIsolation
{
    /// <summary>
    /// The specification's Default Bidi Strategy: a placeholder is wrapped in Unicode isolate
    /// characters (U+2066 to U+2069) unless its direction is known to match the message's.
    /// </summary>
    Default,

    /// <summary>No isolation characters are added.</summary>
    None,
}
