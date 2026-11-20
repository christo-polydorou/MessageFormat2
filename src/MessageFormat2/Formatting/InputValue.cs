using MessageFormat2.Functions;

namespace MessageFormat2.Formatting;

// A caller-supplied value that isn't a MessageValue or a string.
internal sealed class InputValue : MessageValue
{
    private readonly IFormatProvider _formatProvider;

    public InputValue(object? value, IFormatProvider formatProvider)
    {
        Value = value;
        _formatProvider = formatProvider;
    }

    public override string Type => "unknown";

    public override object? Value { get; }

    public override string FormatToString() => Value switch
    {
        null => throw new MessageFunctionException(MessageErrorType.UnsupportedOperation, "A null value cannot be formatted."),
        IFormattable formattable => formattable.ToString(null, _formatProvider),
        _ => Value.ToString() ?? string.Empty,
    };
}
