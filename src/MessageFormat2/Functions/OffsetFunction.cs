using System.Numerics;

using MessageFormat2.Functions.Numbers;

namespace MessageFormat2.Functions;

// :offset (spec/functions/number.md). Adds or subtracts a small integer, keeping the operand's
// formatting options. Exactly one of add and subtract is required.
internal sealed class OffsetFunction : MessageFunction
{
    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        (DecimalNumber number, NumberOptions inherited) = NumberFunction.ReadOperand(operand, ":offset");

        bool hasAdd = options.TryGetValue("add", out MessageValue? add);
        bool hasSubtract = options.TryGetValue("subtract", out MessageValue? subtract);
        if (hasAdd == hasSubtract)
        {
            throw new MessageFunctionException(MessageErrorType.BadOption, ":offset requires exactly one of add and subtract.");
        }

        int amount = NumberOptionParser.ReadDigitSize((add ?? subtract)!, 0, int.MaxValue)
            ?? throw new MessageFunctionException(MessageErrorType.BadOption, "The :offset amount must be a non-negative integer.");

        return new NumberValue(number.Add(hasAdd ? amount : -(BigInteger)amount), inherited, context.Culture);
    }
}
