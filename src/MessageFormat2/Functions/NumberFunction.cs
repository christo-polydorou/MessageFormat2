using MessageFormat2.Functions.Numbers;

namespace MessageFormat2.Functions;

// :number and :integer (spec/functions/number.md). :integer rounds its operand to an integer,
// accepts fewer options, and drops fraction and minimum-significant-digit options inherited from
// its operand.
internal sealed class NumberFunction : MessageFunction
{
    private readonly bool _isInteger;

    private NumberFunction(bool isInteger)
    {
        _isInteger = isInteger;
    }

    public static NumberFunction Number { get; } = new(isInteger: false);

    public static NumberFunction Integer { get; } = new(isInteger: true);

    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        (DecimalNumber number, NumberOptions inherited) = ReadOperand(operand, _isInteger ? ":integer" : ":number");

        // A percent or currency operand is formatted as a plain number.
        inherited = inherited with { Style = NumberStyle.Decimal, Currency = null };

        if (_isInteger)
        {
            inherited = inherited with { MinimumFractionDigits = null, MaximumFractionDigits = null, MinimumSignificantDigits = null };
            number = number.Round(0, RoundingMode.HalfExpand);
        }

        NumberOptions resolved = NumberOptionParser.Parse(
            inherited, options, context, _isInteger ? NumberOptionParser.IntegerOptionNames : NumberOptionParser.NumberOptionNames);
        return new NumberValue(number, resolved, context.Culture);
    }

    // Accepts a NumberValue (keeping its options), a .NET number, or a string matching
    // number-literal.
    public static (DecimalNumber Number, NumberOptions Options) ReadOperand(MessageValue? operand, string function)
    {
        if (operand is NumberValue number)
        {
            return (number.Number, number.Options);
        }

        if (operand is not null && DecimalNumber.TryFrom(operand.Value, out DecimalNumber value))
        {
            return (value, NumberOptions.Default);
        }

        throw new MessageFunctionException(MessageErrorType.BadOperand, $"{function} requires a numeric operand.");
    }
}
