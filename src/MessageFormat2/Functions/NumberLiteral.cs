using System.Globalization;

namespace MessageFormat2.Functions;

// number-literal from spec/functions/number.md, used for string operands of numeric functions:
// ["-"] (%x30 / (%x31-39 *DIGIT)) ["." 1*DIGIT] [%i"e" ["-" / "+"] 1*DIGIT]
internal static class NumberLiteral
{
    public static bool IsMatch(ReadOnlySpan<char> text)
    {
        int i = 0;

        if (i < text.Length && text[i] == '-')
        {
            i++;
        }

        // Integer part: "0", or a non-zero digit followed by any digits.
        if (i >= text.Length || !char.IsAsciiDigit(text[i]))
        {
            return false;
        }

        if (text[i++] != '0')
        {
            SkipDigits(text, ref i);
        }

        if (i < text.Length && text[i] == '.')
        {
            i++;
            if (!SkipDigits(text, ref i))
            {
                return false;
            }
        }

        if (i < text.Length && text[i] is 'e' or 'E')
        {
            i++;
            if (i < text.Length && text[i] is '-' or '+')
            {
                i++;
            }

            if (!SkipDigits(text, ref i))
            {
                return false;
            }
        }

        return i == text.Length;
    }

    public static bool TryParse(string text, out double value)
    {
        if (!IsMatch(text))
        {
            value = 0;
            return false;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool SkipDigits(ReadOnlySpan<char> text, ref int i)
    {
        int start = i;
        while (i < text.Length && char.IsAsciiDigit(text[i]))
        {
            i++;
        }

        return i > start;
    }
}
