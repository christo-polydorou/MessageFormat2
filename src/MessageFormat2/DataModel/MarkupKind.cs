namespace MessageFormat2.DataModel;

/// <summary>The kind of a <see cref="Markup"/> placeholder.</summary>
public enum MarkupKind
{
    /// <summary>An opening element, such as <c>{#bold}</c>.</summary>
    Open,

    /// <summary>A self-closing element, such as <c>{#image/}</c>.</summary>
    Standalone,

    /// <summary>A closing element, such as <c>{/bold}</c>.</summary>
    Close,
}
