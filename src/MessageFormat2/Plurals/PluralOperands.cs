namespace MessageFormat2.Plurals;

// The operands that CLDR plural rules test (TR35, "Plural Operand Meanings").
// 1.50 has i = 1, v = 2, f = 50, t = 5, w = 1.
// They depend on the number as displayed: "1" and "1.0" can fall in different categories, so they
// are computed after fraction digit options are applied.
internal readonly struct PluralOperands
{
    // Integer operands larger than this are reduced so they fit in a decimal. Reduction keeps every
    // mod result the rules use (the largest modulus is 1000000) and keeps the value larger than any
    // constant they compare with.
    private static readonly decimal Reduction = 1_000_000_000_000_000_000_000_000_000m; // 10^27

    private PluralOperands(decimal i, int v, decimal f, int w, decimal t)
    {
        I = i;
        V = v;
        F = f;
        W = w;
        T = t;
        N = v == 0 ? i : i + f / Pow10(v);
    }

    // absolute value
    public decimal N { get; }

    // integer digits
    public decimal I { get; }

    // number of visible fraction digits, with trailing zeros
    public int V { get; }

    // number of visible fraction digits, without trailing zeros
    public int W { get; }

    // visible fraction digits as an integer, with trailing zeros
    public decimal F { get; }

    // visible fraction digits as an integer, without trailing zeros
    public decimal T { get; }

    // compact decimal exponent. Always 0: compact notation isn't supported.
    public int E { get; }

    // Takes the digits of a formatted number, without sign or separators. integerDigits must not be
    // empty.
    public static PluralOperands FromDigits(ReadOnlySpan<char> integerDigits, ReadOnlySpan<char> fractionDigits)
    {
        ReadOnlySpan<char> trimmedFraction = fractionDigits.TrimEnd('0');

        return new PluralOperands(
            i: ToDecimal(integerDigits),
            v: Math.Min(fractionDigits.Length, 27),
            f: ToDecimal(fractionDigits[..Math.Min(fractionDigits.Length, 27)]),
            w: Math.Min(trimmedFraction.Length, 27),
            t: ToDecimal(trimmedFraction[..Math.Min(trimmedFraction.Length, 27)]));
    }

    private static decimal ToDecimal(ReadOnlySpan<char> digits)
    {
        if (digits.Length <= 27)
        {
            return digits.IsEmpty ? 0 : decimal.Parse(digits, provider: System.Globalization.CultureInfo.InvariantCulture);
        }

        // Keep the last 27 digits and add 10^27, so the value stays larger than any rule constant.
        return decimal.Parse(digits[^27..], provider: System.Globalization.CultureInfo.InvariantCulture) + Reduction;
    }

    private static decimal Pow10(int exponent)
    {
        decimal result = 1;
        for (int i = 0; i < exponent; i++)
        {
            result *= 10;
        }

        return result;
    }
}
