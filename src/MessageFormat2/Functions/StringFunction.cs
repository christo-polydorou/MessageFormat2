namespace MessageFormat2.Functions;

// :string (spec/functions/string.md). No options; selects by exact match.
internal sealed class StringFunction : MessageFunction
{
    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        string text = operand switch
        {
            StringValue value => value.Text,
            { Value: string value } => value,
            { Value: IFormattable formattable } => formattable.ToString(null, context.Culture),
            { Value: { } value } => value.ToString() ?? string.Empty,
            _ => throw new MessageFunctionException(MessageErrorType.BadOperand, ":string requires an operand that can be converted to text."),
        };

        return new StringValue(text, context.Locale);
    }
}
