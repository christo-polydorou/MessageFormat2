using System.Collections.Frozen;
using System.Globalization;

namespace MessageFormat2.Functions.Numbers;

// Reads options on top of any carried by the operand. Invalid values are reported as Bad Option and
// ignored.
internal static class NumberOptionParser
{
    public static readonly FrozenSet<string> NumberOptionNames = new[]
    {
        "select", "signDisplay", "useGrouping", "minimumIntegerDigits", "minimumFractionDigits", "maximumFractionDigits",
        "minimumSignificantDigits", "maximumSignificantDigits", "trailingZeroDisplay", "roundingPriority",
        "roundingIncrement", "roundingMode"
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> IntegerOptionNames = new[]
    {
        "select", "signDisplay", "useGrouping", "minimumIntegerDigits", "maximumSignificantDigits"
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> PercentOptionNames = new[]
    {
        "signDisplay", "useGrouping", "minimumFractionDigits", "maximumFractionDigits", "minimumSignificantDigits",
        "maximumSignificantDigits", "trailingZeroDisplay", "roundingPriority", "roundingMode"
    }.ToFrozenSet(StringComparer.Ordinal);

    // "currency" isn't in here; the function reads that one itself.
    public static readonly FrozenSet<string> CurrencyOptionNames = new[]
    {
        "currencySign", "currencyDisplay", "useGrouping", "minimumIntegerDigits", "fractionDigits",
        "minimumSignificantDigits", "maximumSignificantDigits", "trailingZeroDisplay", "roundingPriority",
        "roundingIncrement", "roundingMode"
    }.ToFrozenSet(StringComparer.Ordinal);

    // accepted is the set of options this function has. Others are ignored, as for any unknown
    // option.
    public static NumberOptions Parse(
        NumberOptions inherited, MessageOptionDictionary options, MessageFunctionContext context, FrozenSet<string> accepted)
    {
        NumberOptions result = inherited;

        // A selector's keys depend on its select mode, so the mode must be written in the
        // message itself, not inherited from an operand or taken from a variable.
        if (accepted.Contains("select") && inherited.SelectIsSet && !options.ContainsKey("select"))
        {
            context.ReportError(MessageErrorType.BadOption, "The select option cannot come from the operand.");
            result = result with { SelectIsSet = false, SelectionAllowed = false };
        }

        foreach ((string name, MessageValue value) in options)
        {
            if (!accepted.Contains(name))
            {
                continue;
            }

            result = name switch
            {
                "select" => ParseSelect(result, value, options.IsLiteral(name), context),
                "signDisplay" => Enum(context, name, value, result, SignDisplays, (r, v) => r with { SignDisplay = v }),
                "useGrouping" => Enum(context, name, value, result, UseGroupings, (r, v) => r with { UseGrouping = v }),
                "trailingZeroDisplay" => Enum(context, name, value, result, TrailingZeroDisplays, (r, v) => r with { StripIfInteger = v }),
                "roundingPriority" => Enum(context, name, value, result, RoundingPriorities, (r, v) => r with { RoundingPriority = v }),
                "roundingMode" => Enum(context, name, value, result, RoundingModes, (r, v) => r with { RoundingMode = v }),
                "roundingIncrement" => Enum(context, name, value, result, RoundingIncrements, (r, v) => r with { RoundingIncrement = v }),
                "minimumIntegerDigits" => Digits(context, name, value, result, 1, 21, (r, v) => r with { MinimumIntegerDigits = v }),
                "minimumFractionDigits" => Digits(context, name, value, result, 0, 100, (r, v) => r with { MinimumFractionDigits = v }),
                "maximumFractionDigits" => Digits(context, name, value, result, 0, 100, (r, v) => r with { MaximumFractionDigits = v }),
                "minimumSignificantDigits" => Digits(context, name, value, result, 1, 21, (r, v) => r with { MinimumSignificantDigits = v }),
                "maximumSignificantDigits" => Digits(context, name, value, result, 1, 21, (r, v) => r with { MaximumSignificantDigits = v }),
                "fractionDigits" => ParseFractionDigits(result, value, context),
                "currencySign" => Enum(context, name, value, result, CurrencySigns, (r, v) => r with { AccountingSign = v }),
                "currencyDisplay" => Enum(context, name, value, result, CurrencyDisplays, (r, v) => r with { CurrencyDisplay = v }),
                _ => result,
            };
        }

        return result;
    }

    // A non-negative integer, given as a string matching "0" / (("1"-"9") [DIGIT]) or as an
    // integral number. Out-of-range values are clamped, which the spec allows.
    public static int? ReadDigitSize(MessageValue value, int minimum, int maximum)
    {
        int? digits = value.Value switch
        {
            string text when text.Length is 1 or 2 && text.All(char.IsAsciiDigit) && (text.Length == 1 || text[0] != '0')
                => int.Parse(text, CultureInfo.InvariantCulture),
            int i when i >= 0 => i,
            long l when l >= 0 => (int)Math.Min(l, int.MaxValue),
            decimal m when m >= 0 && m == decimal.Truncate(m) => (int)Math.Min(m, int.MaxValue),
            double d when d >= 0 && d == Math.Truncate(d) => (int)Math.Min(d, int.MaxValue),
            _ => null,
        };

        return digits is null ? null : Math.Clamp(digits.Value, minimum, maximum);
    }

    private static NumberOptions ParseSelect(NumberOptions result, MessageValue value, bool isLiteral, MessageFunctionContext context)
    {
        if (!isLiteral)
        {
            context.ReportError(MessageErrorType.BadOption, "The select option must be a literal.");
            return result with { SelectionAllowed = false };
        }

        return Enum(context, "select", value, result, Selects, (r, v) => r with { Select = v, SelectIsSet = true });
    }

    // :currency only: auto (the currency's own digits) or an exact count.
    private static NumberOptions ParseFractionDigits(NumberOptions result, MessageValue value, MessageFunctionContext context)
    {
        if (value.Value is "auto")
        {
            return result with { MinimumFractionDigits = null, MaximumFractionDigits = null };
        }

        return Digits(context, "fractionDigits", value, result, 0, 100, (r, v) => r with { MinimumFractionDigits = v, MaximumFractionDigits = v });
    }

    private static NumberOptions Enum<T>(
        MessageFunctionContext context, string name, MessageValue value, NumberOptions result,
        Dictionary<string, T> allowed, Func<NumberOptions, T, NumberOptions> apply)
    {
        if (value.Value is string text && allowed.TryGetValue(text, out T? parsed))
        {
            return apply(result, parsed);
        }

        context.ReportError(MessageErrorType.BadOption, $"'{value.Value}' is not a valid value for {name}.");
        return result;
    }

    private static NumberOptions Digits(
        MessageFunctionContext context, string name, MessageValue value, NumberOptions result,
        int minimum, int maximum, Func<NumberOptions, int, NumberOptions> apply)
    {
        if (ReadDigitSize(value, minimum, maximum) is { } digits)
        {
            return apply(result, digits);
        }

        context.ReportError(MessageErrorType.BadOption, $"'{value.Value}' is not a valid digit size for {name}.");
        return result;
    }

    private static readonly Dictionary<string, NumberSelect> Selects = new(StringComparer.Ordinal)
    {
        ["plural"] = NumberSelect.Plural,
        ["ordinal"] = NumberSelect.Ordinal,
        ["exact"] = NumberSelect.Exact,
    };

    private static readonly Dictionary<string, SignDisplay> SignDisplays = new(StringComparer.Ordinal)
    {
        ["auto"] = SignDisplay.Auto,
        ["always"] = SignDisplay.Always,
        ["exceptZero"] = SignDisplay.ExceptZero,
        ["negative"] = SignDisplay.Negative,
        ["never"] = SignDisplay.Never,
    };

    private static readonly Dictionary<string, UseGrouping> UseGroupings = new(StringComparer.Ordinal)
    {
        ["auto"] = UseGrouping.Auto,
        ["always"] = UseGrouping.Always,
        ["never"] = UseGrouping.Never,
        ["min2"] = UseGrouping.Min2,
    };

    private static readonly Dictionary<string, bool> TrailingZeroDisplays = new(StringComparer.Ordinal)
    {
        ["auto"] = false,
        ["stripIfInteger"] = true,
    };

    private static readonly Dictionary<string, bool> CurrencySigns = new(StringComparer.Ordinal)
    {
        ["standard"] = false,
        ["accounting"] = true,
    };

    private static readonly Dictionary<string, CurrencyDisplay> CurrencyDisplays = new(StringComparer.Ordinal)
    {
        ["symbol"] = CurrencyDisplay.Symbol,
        ["narrowSymbol"] = CurrencyDisplay.NarrowSymbol,
        ["name"] = CurrencyDisplay.Name,
        ["code"] = CurrencyDisplay.Code,
        ["never"] = CurrencyDisplay.Never,
    };

    private static readonly Dictionary<string, RoundingPriority> RoundingPriorities = new(StringComparer.Ordinal)
    {
        ["auto"] = RoundingPriority.Auto,
        ["morePrecision"] = RoundingPriority.MorePrecision,
        ["lessPrecision"] = RoundingPriority.LessPrecision,
    };

    private static readonly Dictionary<string, RoundingMode> RoundingModes = new(StringComparer.Ordinal)
    {
        ["ceil"] = RoundingMode.Ceil,
        ["floor"] = RoundingMode.Floor,
        ["expand"] = RoundingMode.Expand,
        ["trunc"] = RoundingMode.Trunc,
        ["halfCeil"] = RoundingMode.HalfCeil,
        ["halfFloor"] = RoundingMode.HalfFloor,
        ["halfExpand"] = RoundingMode.HalfExpand,
        ["halfTrunc"] = RoundingMode.HalfTrunc,
        ["halfEven"] = RoundingMode.HalfEven,
    };

    private static readonly Dictionary<string, int> RoundingIncrements = new[] { 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000 }
        .ToDictionary(increment => increment.ToString(CultureInfo.InvariantCulture), increment => increment, StringComparer.Ordinal);
}
