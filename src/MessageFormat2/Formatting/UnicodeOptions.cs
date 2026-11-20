using MessageFormat2.Functions;

namespace MessageFormat2.Formatting;

// The u: options of one expression or markup. They're taken out of the options before the function
// is called.
internal readonly record struct UnicodeOptions(string? Id, DirectionOption? Direction)
{
    public const string IdName = "u:id";
    public const string DirectionName = "u:dir";

    // u:dir set to anything but inherit
    public bool Isolate => Direction is DirectionOption.LeftToRight or DirectionOption.RightToLeft or DirectionOption.Auto;

    public TextDirection BaseDirection => Direction switch
    {
        DirectionOption.LeftToRight => TextDirection.LeftToRight,
        DirectionOption.RightToLeft => TextDirection.RightToLeft,
        _ => TextDirection.Unknown,
    };

    public MessageValue ApplyTo(MessageValue value) =>
        Id is null && !Isolate ? value : new AnnotatedValue(value, this);

    public static DirectionOption? ParseDirection(string value) => value switch
    {
        "inherit" => DirectionOption.Inherit,
        "ltr" => DirectionOption.LeftToRight,
        "rtl" => DirectionOption.RightToLeft,
        "auto" => DirectionOption.Auto,
        _ => null,
    };
}
