using MessageFormat2.Functions.Numbers;

namespace MessageFormat2.Functions;

// :currency (spec/functions/number.md). The operand is a number plus the currency option, or the
// result of another :currency expression, whose currency can't be changed. Not usable as a
// selector.
internal sealed class CurrencyFunction : MessageFunction
{
    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        (DecimalNumber number, NumberOptions inherited) = NumberFunction.ReadOperand(operand, ":currency");

        string? currency = inherited.Style == NumberStyle.Currency ? inherited.Currency : null;
        if (options.TryGetValue("currency", out MessageValue? option))
        {
            if (currency is not null)
            {
                context.ReportError(MessageErrorType.BadOption, $"The operand already has the currency {currency}; the currency option cannot change it.");
            }
            else if (option.Value is string code && CurrencyData.IsWellFormedCode(code))
            {
                currency = code.ToUpperInvariant();
            }
            else
            {
                context.ReportError(MessageErrorType.BadOption, $"'{option.Value}' is not a currency code (three letters, such as EUR).");
            }
        }

        if (currency is null)
        {
            throw new MessageFunctionException(MessageErrorType.BadOperand, ":currency requires a currency, given by the currency option.");
        }

        inherited = inherited with { Style = NumberStyle.Currency, Currency = currency };
        NumberOptions resolved = NumberOptionParser.Parse(inherited, options, context, NumberOptionParser.CurrencyOptionNames);
        return new NumberValue(number, resolved, context.Culture);
    }
}
