using MessageFormat2.Functions.Numbers;

namespace MessageFormat2.Functions;

// :percent (spec/functions/number.md). {0.42 :percent} is "42%".
// The resolved value keeps the original number (0.42); only display and selection use the
// multiplied one. Selection always uses plural categories. No fraction digits by default.
internal sealed class PercentFunction : MessageFunction
{
    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        (DecimalNumber number, NumberOptions inherited) = NumberFunction.ReadOperand(operand, ":percent");

        // The spec discards these options when they come from the operand.
        inherited = inherited with
        {
            Style = NumberStyle.Percent,
            Currency = null,
            MinimumIntegerDigits = null,
            RoundingIncrement = 1,
            Select = NumberSelect.Plural,
            SelectIsSet = false,
            SelectionAllowed = true,
        };

        NumberOptions resolved = NumberOptionParser.Parse(inherited, options, context, NumberOptionParser.PercentOptionNames);
        return new NumberValue(number, resolved, context.Culture);
    }
}
