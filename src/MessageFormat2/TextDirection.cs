namespace MessageFormat2;

/// <summary>The direction of a piece of text, used for bidirectional isolation.</summary>
public enum TextDirection
{
    /// <summary>The direction is not known.</summary>
    Unknown,

    /// <summary>Left-to-right, as in English.</summary>
    LeftToRight,

    /// <summary>Right-to-left, as in Arabic or Hebrew.</summary>
    RightToLeft,
}
