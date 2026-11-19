using System.Globalization;
using System.Text;

using MessageFormat2.Formatting;

namespace MessageFormat2.Functions.Numbers;

// A number rounded and laid out for display, before culture-specific symbols are applied. Integer
// has no grouping; Fraction can be empty.
internal readonly record struct RoundedNumber(bool IsNegative, bool IsZero, string Integer, string Fraction);

// Option names and defaults follow JavaScript's Intl.NumberFormat, which is where the spec's
// options come from. For example, the default maximum is three fraction digits.
internal static class NumberFormatter
{
    // .NET's percent and currency patterns, by the index in NumberFormatInfo.
    // n = the number, - = the sign, % = the percent sign, $ = the currency symbol.
    private static readonly string[] PercentPositivePatterns = ["n %", "n%", "%n", "% n"];
    private static readonly string[] PercentNegativePatterns =
        ["-n %", "-n%", "-%n", "%-n", "%n-", "n-%", "n%-", "-% n", "n %-", "% n-", "% -n", "n- %"];
    private static readonly string[] CurrencyPositivePatterns = ["$n", "n$", "$ n", "n $"];
    private static readonly string[] CurrencyNegativePatterns =
        ["($n)", "-$n", "$-n", "$n-", "(n$)", "-n$", "n-$", "n$-", "-n $", "-$ n", "n $-", "$ n-", "$ -n", "n- $", "($ n)", "(n $)", "$- n"];

    // value is already multiplied by 100 for a percentage.
    public static RoundedNumber Round(DecimalNumber value, NumberOptions options, int defaultMinimumFraction = 0, int defaultMaximumFraction = 3)
    {
        // As in Intl.NumberFormat, a maximum below the default minimum lowers the minimum.
        int minimumFraction = options.MinimumFractionDigits
            ?? Math.Min(defaultMinimumFraction, options.MaximumFractionDigits ?? defaultMinimumFraction);
        int maximumFraction = Math.Max(options.MaximumFractionDigits ?? Math.Max(minimumFraction, defaultMaximumFraction), minimumFraction);

        (DecimalNumber byFraction, int byFractionMinimum) = (
            value.Round(-maximumFraction, options.RoundingMode, options.RoundingIncrement),
            minimumFraction);

        DecimalNumber rounded = byFraction;
        int displayMinimum = byFractionMinimum;

        bool useSignificant = options.MinimumSignificantDigits is not null || options.MaximumSignificantDigits is not null;
        if (useSignificant)
        {
            int minimumSignificant = options.MinimumSignificantDigits ?? 1;
            int maximumSignificant = Math.Max(options.MaximumSignificantDigits ?? 21, minimumSignificant);

            int roundingExponent = value.MostSignificantExponent - maximumSignificant + 1;
            DecimalNumber bySignificant = value.Round(roundingExponent, options.RoundingMode);
            int bySignificantMinimum = Math.Max(0, minimumSignificant - bySignificant.MostSignificantExponent - 1);

            bool chooseSignificant = options.RoundingPriority switch
            {
                RoundingPriority.MorePrecision => roundingExponent < -maximumFraction,
                RoundingPriority.LessPrecision => roundingExponent > -maximumFraction,
                _ => true,
            };

            if (chooseSignificant)
            {
                rounded = bySignificant;
                displayMinimum = bySignificantMinimum;
            }
        }

        (string integer, string fraction) = rounded.ToDigits(displayMinimum);

        if (options.StripIfInteger && fraction.TrimEnd('0').Length == 0)
        {
            fraction = "";
        }

        integer = integer.PadLeft(options.MinimumIntegerDigits ?? 1, '0');
        return new RoundedNumber(rounded.IsNegative, rounded.IsZero, integer, fraction);
    }

    // currencySymbol is the symbol or code to show, or null to show none.
    public static List<ExpressionSubpart> ToParts(RoundedNumber number, NumberOptions options, NumberFormatInfo format, string? currencySymbol = null)
    {
        string? sign = options.SignDisplay switch
        {
            SignDisplay.Auto => number.IsNegative ? format.NegativeSign : null,
            SignDisplay.Always => number.IsNegative ? format.NegativeSign : format.PositiveSign,
            SignDisplay.ExceptZero when number.IsZero => null,
            SignDisplay.ExceptZero => number.IsNegative ? format.NegativeSign : format.PositiveSign,
            SignDisplay.Negative => number.IsNegative && !number.IsZero ? format.NegativeSign : null,
            _ => null,
        };

        bool isMinus = sign is not null && number.IsNegative;
        string pattern = Pattern(options, format, sign is not null, isMinus, currencySymbol);

        if (options.AccountingSign && isMinus)
        {
            sign = null; // Shown by the parentheses.
        }

        var parts = new List<ExpressionSubpart>();
        foreach (char token in pattern)
        {
            switch (token)
            {
                case 'n':
                    AddNumberParts(parts, number, options, format);
                    break;
                case '-' when sign is not null:
                    parts.Add(new ExpressionSubpart(isMinus ? "minusSign" : "plusSign", sign));
                    break;
                case '%':
                    parts.Add(new ExpressionSubpart("percentSign", format.PercentSymbol));
                    break;
                case '$':
                    parts.Add(new ExpressionSubpart("currency", currencySymbol!));
                    break;
                case '_':
                    parts.Add(new ExpressionSubpart("literal", "\u00A0"));
                    break;
                case ' ' or '(' or ')':
                    parts.Add(new ExpressionSubpart("literal", token.ToString()));
                    break;
            }
        }

        return parts;
    }

    // The layout of sign, number and symbol, from the culture's patterns: "-n" for a negative
    // number, "n %" for a percentage in French, "$n" for a currency in English.
    private static string Pattern(NumberOptions options, NumberFormatInfo format, bool hasSign, bool isMinus, string? currencySymbol)
    {
        switch (options.Style)
        {
            case NumberStyle.Percent:
                return hasSign
                    ? PatternAt(PercentNegativePatterns, format.PercentNegativePattern)
                    : PatternAt(PercentPositivePatterns, format.PercentPositivePattern);

            case NumberStyle.Currency:
                string positive = PatternAt(CurrencyPositivePatterns, format.CurrencyPositivePattern);
                string negative = PatternAt(CurrencyNegativePatterns, format.CurrencyNegativePattern);
                string pattern = (hasSign, isMinus && options.AccountingSign) switch
                {
                    (false, _) => positive,
                    (true, true) => "(" + positive + ")",

                    // Some cultures show negative amounts in parentheses; with currencySign=standard, use a sign.
                    _ => negative.Contains('-', StringComparison.Ordinal) ? negative : "-" + positive,
                };
                return CurrencySpacing(pattern, currencySymbol);

            default:
                return hasSign ? "-n" : "n";
        }
    }

    private static string PatternAt(string[] patterns, int index) =>
        (uint)index < (uint)patterns.Length ? patterns[index] : patterns[0];

    // Removes the symbol when there is none to show, and separates a symbol that ends in a letter
    // (such as CHF) from the digits with a no-break space (_), as CLDR's currency spacing rules do.
    private static string CurrencySpacing(string pattern, string? symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return pattern.Replace("$ ", "", StringComparison.Ordinal).Replace(" $", "", StringComparison.Ordinal).Replace("$", "", StringComparison.Ordinal);
        }

        if (char.IsLetter(symbol[^1]))
        {
            pattern = pattern.Replace("$n", "$_n", StringComparison.Ordinal);
        }

        if (char.IsLetter(symbol[0]))
        {
            pattern = pattern.Replace("n$", "n_$", StringComparison.Ordinal);
        }

        return pattern;
    }

    private static void AddNumberParts(List<ExpressionSubpart> parts, RoundedNumber number, NumberOptions options, NumberFormatInfo format)
    {
        (string decimalSeparator, string groupSeparator, int[] groupSizes) = options.Style switch
        {
            NumberStyle.Percent => (format.PercentDecimalSeparator, format.PercentGroupSeparator, format.PercentGroupSizes),
            NumberStyle.Currency => (format.CurrencyDecimalSeparator, format.CurrencyGroupSeparator, format.CurrencyGroupSizes),
            _ => (format.NumberDecimalSeparator, format.NumberGroupSeparator, format.NumberGroupSizes),
        };

        AddIntegerParts(parts, number.Integer, options.UseGrouping, groupSeparator, groupSizes);

        if (number.Fraction.Length > 0)
        {
            parts.Add(new ExpressionSubpart("decimal", decimalSeparator));
            parts.Add(new ExpressionSubpart("fraction", number.Fraction));
        }
    }

    // e.g. -1234.50. Used to match numeric variant keys.
    public static string ToInvariantString(RoundedNumber number)
    {
        var text = new StringBuilder();
        if (number.IsNegative && !number.IsZero)
        {
            text.Append('-');
        }

        text.Append(number.Integer.TrimStart('0').PadLeft(1, '0'));
        if (number.Fraction.Length > 0)
        {
            text.Append('.').Append(number.Fraction);
        }

        return text.ToString();
    }

    // Uses the culture's group sizes (3, or 3 then 2 in India).
    private static void AddIntegerParts(List<ExpressionSubpart> parts, string digits, UseGrouping useGrouping, string separator, int[] sizes)
    {
        bool group = useGrouping switch
        {
            UseGrouping.Never => false,
            UseGrouping.Min2 => sizes.Length > 0 && sizes[0] > 0 && digits.Length >= sizes[0] + 2,
            _ => true,
        };

        if (!group || sizes.Length == 0 || sizes[0] <= 0)
        {
            parts.Add(new ExpressionSubpart("integer", digits));
            return;
        }

        // Work from the right: the first size is the group nearest the decimal point; the last size repeats, and 0 stops grouping.
        var groups = new List<string>();
        int end = digits.Length;
        int sizeIndex = 0;
        while (end > 0)
        {
            int size = sizes[Math.Min(sizeIndex, sizes.Length - 1)];
            if (size <= 0 || end <= size)
            {
                groups.Add(digits[..end]);
                break;
            }

            groups.Add(digits[(end - size)..end]);
            end -= size;
            sizeIndex++;
        }

        groups.Reverse();
        for (int i = 0; i < groups.Count; i++)
        {
            if (i > 0)
            {
                parts.Add(new ExpressionSubpart("group", separator));
            }

            parts.Add(new ExpressionSubpart("integer", groups[i]));
        }
    }
}
