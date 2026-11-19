using System.Globalization;

using MessageFormat2.Formatting;
using MessageFormat2.Functions.Numbers;
using MessageFormat2.Plurals;

namespace MessageFormat2.Functions;

/// <summary>
/// A number with its formatting options: the result of <c>:number</c>, <c>:integer</c>,
/// <c>:offset</c>, <c>:percent</c>, or <c>:currency</c>, and of any numeric argument used without a function.
/// Selects by exact numeric key first, then by CLDR plural or ordinal category of the number as displayed.
/// </summary>
public sealed class NumberValue : MessageValue
{
    private readonly CultureInfo _culture;
    private readonly RoundedNumber _rounded;
    private readonly string? _currencySymbol;

    internal NumberValue(DecimalNumber number, NumberOptions options, CultureInfo culture)
    {
        Number = number;
        Options = options;
        _culture = culture;

        DecimalNumber displayed = number;
        (int minimumFraction, int maximumFraction) = (0, 3);
        switch (options.Style)
        {
            case NumberStyle.Percent:
                displayed = number.ScaleByPowerOfTen(2);
                maximumFraction = 0;
                break;
            case NumberStyle.Currency:
                minimumFraction = maximumFraction = CurrencyData.FractionDigits(options.Currency!, culture);
                _currencySymbol = CurrencyData.Symbol(options.Currency!, options.CurrencyDisplay, culture);
                break;
        }

        _rounded = NumberFormatter.Round(displayed, options, minimumFraction, maximumFraction);
    }

    // exact, before rounding for display
    internal DecimalNumber Number { get; }

    internal NumberOptions Options { get; }

    /// <inheritdoc/>
    public override string Type => "number";

    /// <summary>The number, as a <see cref="decimal"/> if it fits exactly, otherwise as a <see cref="double"/>.</summary>
    public override object? Value => Number.ToClrValue();

    /// <inheritdoc/>
    public override string? Locale => _culture.Name;

    /// <inheritdoc/>
    public override TextDirection Direction =>
        _culture.TextInfo.IsRightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;

    /// <inheritdoc/>
    public override string FormatToString() => string.Concat(FormatToParts().Select(part => part.Value));

    /// <inheritdoc/>
    public override IReadOnlyList<ExpressionSubpart> FormatToParts() =>
        NumberFormatter.ToParts(_rounded, Options, _culture.NumberFormat, _currencySymbol);

    /// <inheritdoc/>
    public override bool SupportsSelection => Options.SelectionAllowed && Options.Style != NumberStyle.Currency;

    /// <summary>
    /// Matches a numeric key (<c>1</c>, <c>-2.5</c>) exactly, or a plural category key (<c>one</c>)
    /// by the culture's rules. Any other key is a Bad Variant Key.
    /// </summary>
    public override bool Matches(string key)
    {
        if (NumberLiteral.IsMatch(key))
        {
            return key == ExactKey();
        }

        if (key is "zero" or "one" or "two" or "few" or "many" or "other")
        {
            return Options.Select != NumberSelect.Exact && key == PluralRules.ToKey(Category());
        }

        throw new MessageFunctionException(MessageErrorType.BadVariantKey, $"'{key}' is not a number or a plural category.");
    }

    /// <summary>An exact numeric key is a better match than a plural category.</summary>
    public override bool IsBetterMatch(string key1, string key2) =>
        NumberLiteral.IsMatch(key1) && !NumberLiteral.IsMatch(key2);

    // spec: "Exact Literal Match Serialization". Integers without digit options serialize as plain
    // integers; anything else uses the displayed digits.
    private string ExactKey() => NumberFormatter.ToInvariantString(_rounded);

    private PluralCategory Category()
    {
        // Plural rules look at the displayed digits, ignoring padding added by minimumIntegerDigits.
        string integer = _rounded.Integer.TrimStart('0').PadLeft(1, '0');
        PluralOperands operands = PluralOperands.FromDigits(integer, _rounded.Fraction);

        return Options.Select == NumberSelect.Ordinal
            ? PluralRules.SelectOrdinal(_culture, operands)
            : PluralRules.SelectCardinal(_culture, operands);
    }
}
