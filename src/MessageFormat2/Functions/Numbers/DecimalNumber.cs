using System.Globalization;
using System.Numerics;

namespace MessageFormat2.Functions.Numbers;

// An exact decimal: ±Magnitude × 10^Exponent. All numeric formatting and selection goes through
// this, so rounding is exact in base 10.
internal readonly struct DecimalNumber
{
    // Larger exponents are rejected, to keep the output a sensible size.
    private const int MaxExponent = 1000;

    private DecimalNumber(BigInteger coefficient, int exponent, bool isNegative)
    {
        // Normalize: move trailing zeros of the coefficient into the exponent.
        if (coefficient.IsZero)
        {
            exponent = 0;
        }
        else
        {
            while (coefficient % 10 == 0)
            {
                coefficient /= 10;
                exponent++;
            }
        }

        Magnitude = BigInteger.Abs(coefficient);
        Exponent = exponent;
        IsNegative = isNegative;
    }

    public BigInteger Magnitude { get; }

    public int Exponent { get; }

    // also true for negative zero
    public bool IsNegative { get; }

    public bool IsZero => Magnitude.IsZero;

    public static bool TryParse(string text, out DecimalNumber number)
    {
        number = default;
        if (!NumberLiteral.IsMatch(text))
        {
            return false;
        }

        bool negative = text.StartsWith('-');
        ReadOnlySpan<char> rest = negative ? text.AsSpan(1) : text.AsSpan();

        int exponent = 0;
        int e = rest.IndexOfAny('e', 'E');
        if (e >= 0)
        {
            if (!int.TryParse(rest[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
            {
                return false;
            }

            rest = rest[..e];
        }

        int dot = rest.IndexOf('.');
        string digits = dot < 0 ? rest.ToString() : string.Concat(rest[..dot], rest[(dot + 1)..]);
        if (dot >= 0)
        {
            exponent -= rest.Length - dot - 1;
        }

        if (Math.Abs((long)exponent) > MaxExponent)
        {
            return false;
        }

        number = new DecimalNumber(BigInteger.Parse(digits, CultureInfo.InvariantCulture), exponent, negative);
        return true;
    }

    public static bool TryFrom(object? value, out DecimalNumber number)
    {
        switch (value)
        {
            case string text:
                return TryParse(text, out number);
            case int or long or short or sbyte or byte or ushort or uint or ulong or BigInteger or Int128 or UInt128:
                var integer = BigInteger.Parse(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                number = new DecimalNumber(integer, 0, integer.Sign < 0);
                return true;
            case decimal m:
                return TryParse(m.ToString(CultureInfo.InvariantCulture), out number);
            case double d:
                return TryFromBinary(d, d.ToString("R", CultureInfo.InvariantCulture), out number);
            case float f:
                return TryFromBinary(f, f.ToString("R", CultureInfo.InvariantCulture), out number);
            case Half h:
                return TryFromBinary((double)h, h.ToString(CultureInfo.InvariantCulture), out number);
            default:
                number = default;
                return false;
        }
    }

    // Goes through the shortest round-trip string, as JavaScript does.
    private static bool TryFromBinary(double value, string text, out DecimalNumber number)
    {
        number = default;
        if (!double.IsFinite(value))
        {
            return false;
        }

        // "R" formatting uses forms like "1E+20" and "1.5E-05"; number-literal wants "1e20" and "1.5e-05".
        string literal = text.Replace("E+", "e", StringComparison.Ordinal).Replace('E', 'e');
        if (!TryParse(literal, out number))
        {
            return false;
        }

        // Keep the sign of negative zero.
        number = new DecimalNumber(number.Magnitude, number.Exponent, double.IsNegative(value));
        return true;
    }

    public DecimalNumber Add(BigInteger amount)
    {
        BigInteger signed = IsNegative ? -Magnitude : Magnitude;
        int exponent = Math.Min(Exponent, 0);
        BigInteger sum = signed * BigInteger.Pow(10, Exponent - exponent) + amount * BigInteger.Pow(10, -exponent);
        return new DecimalNumber(sum, exponent, sum.Sign < 0);
    }

    public DecimalNumber ScaleByPowerOfTen(int power) => new(Magnitude, Exponent + power, IsNegative);

    // Rounds to a multiple of increment × 10^exponent, e.g. exponent = -2 for two fraction digits.
    public DecimalNumber Round(int exponent, RoundingMode mode, int increment = 1)
    {
        if (Exponent >= exponent && increment == 1)
        {
            return this;
        }

        // Divide |value| by increment × 10^exponent: numerator / denominator, both integers.
        int shift = Exponent - exponent;
        BigInteger numerator = shift >= 0 ? Magnitude * BigInteger.Pow(10, shift) : Magnitude;
        BigInteger denominator = (shift >= 0 ? BigInteger.One : BigInteger.Pow(10, -shift)) * increment;

        BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
        if (ShouldRoundUp(mode, quotient, remainder, denominator))
        {
            quotient++;
        }

        return new DecimalNumber(quotient * increment, exponent, IsNegative);
    }

    private bool ShouldRoundUp(RoundingMode mode, BigInteger quotient, BigInteger remainder, BigInteger denominator)
    {
        if (remainder.IsZero)
        {
            return false;
        }

        int half = (remainder * 2).CompareTo(denominator); // < 0 below half, 0 exactly half, > 0 above
        return mode switch
        {
            RoundingMode.Ceil => !IsNegative,
            RoundingMode.Floor => IsNegative,
            RoundingMode.Expand => true,
            RoundingMode.Trunc => false,
            RoundingMode.HalfCeil => half > 0 || (half == 0 && !IsNegative),
            RoundingMode.HalfFloor => half > 0 || (half == 0 && IsNegative),
            RoundingMode.HalfExpand => half >= 0,
            RoundingMode.HalfTrunc => half > 0,
            RoundingMode.HalfEven => half > 0 || (half == 0 && !quotient.IsEven),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    // 2 for 123, -2 for 0.05, 0 for zero
    public int MostSignificantExponent =>
        IsZero ? 0 : Exponent + Magnitude.ToString(CultureInfo.InvariantCulture).Length - 1;

    // The digits of the absolute value, split at the decimal point. The fraction is padded with
    // zeros up to minimumFractionDigits and has no trailing zeros beyond it.
    public (string Integer, string Fraction) ToDigits(int minimumFractionDigits)
    {
        string digits = Magnitude.ToString(CultureInfo.InvariantCulture);
        string integer;
        string fraction;

        if (Exponent >= 0)
        {
            integer = IsZero ? "0" : digits + new string('0', Exponent);
            fraction = "";
        }
        else
        {
            int fractionLength = -Exponent;
            digits = digits.PadLeft(fractionLength + 1, '0');
            integer = digits[..^fractionLength];
            fraction = digits[^fractionLength..].TrimEnd('0');
        }

        return (integer, fraction.PadRight(minimumFractionDigits, '0'));
    }

    // decimal if it fits exactly, otherwise double
    public object ToClrValue()
    {
        (string integer, string fraction) = ToDigits(0);
        string text = (IsNegative && !IsZero ? "-" : "") + integer + (fraction.Length > 0 ? "." + fraction : "");

        return integer.Length + fraction.Length <= 28 && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal m)
            ? m
            : double.Parse(text, CultureInfo.InvariantCulture);
    }
}
