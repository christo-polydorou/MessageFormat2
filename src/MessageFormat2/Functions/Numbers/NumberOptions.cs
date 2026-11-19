namespace MessageFormat2.Functions.Numbers;

internal enum NumberStyle
{
    Decimal,
    Percent,
    Currency,
}

internal enum CurrencyDisplay
{
    Symbol,
    NarrowSymbol,

    // .NET has no localized currency names, so this shows the code, which the spec allows.
    Name,
    Code,
    Never,
}

internal enum NumberSelect
{
    // exact numeric keys, then CLDR cardinal categories (one, few, ...)
    Plural,

    // exact numeric keys, then CLDR ordinal categories (1st, 2nd, 3rd)
    Ordinal,

    Exact,
}

internal enum SignDisplay
{
    Auto,
    Always,
    ExceptZero,
    Negative,
    Never,
}

internal enum UseGrouping
{
    Auto,
    Always,
    Never,
    Min2,
}

internal enum RoundingPriority
{
    Auto,
    MorePrecision,
    LessPrecision,
}

internal enum RoundingMode
{
    Ceil,
    Floor,
    Expand,
    Trunc,
    HalfCeil,
    HalfFloor,
    HalfExpand,
    HalfTrunc,
    HalfEven,
}

// Null means the option wasn't set, so the default applies.
// A value carries its options with it: {$x :number} where $x was declared as {$n :number
// minimumFractionDigits=2} keeps two fraction digits.
internal sealed record NumberOptions
{
    public static NumberOptions Default { get; } = new();

    public NumberStyle Style { get; init; } = NumberStyle.Decimal;

    // ISO 4217, upper case. Only set for NumberStyle.Currency.
    public string? Currency { get; init; }

    public CurrencyDisplay CurrencyDisplay { get; init; } = CurrencyDisplay.Symbol;

    // currencySign=accounting: negative amounts in parentheses
    public bool AccountingSign { get; init; }

    public NumberSelect Select { get; init; } = NumberSelect.Plural;

    // select was set explicitly. Such a value can't be the operand of a new selector.
    public bool SelectIsSet { get; init; }

    // False when select was set from a variable, which makes the value unusable for selection.
    public bool SelectionAllowed { get; init; } = true;

    public SignDisplay SignDisplay { get; init; } = SignDisplay.Auto;

    public UseGrouping UseGrouping { get; init; } = UseGrouping.Auto;

    public int? MinimumIntegerDigits { get; init; }

    public int? MinimumFractionDigits { get; init; }

    public int? MaximumFractionDigits { get; init; }

    public int? MinimumSignificantDigits { get; init; }

    public int? MaximumSignificantDigits { get; init; }

    public bool StripIfInteger { get; init; }

    public RoundingPriority RoundingPriority { get; init; } = RoundingPriority.Auto;

    public int RoundingIncrement { get; init; } = 1;

    public RoundingMode RoundingMode { get; init; } = RoundingMode.HalfExpand;
}
