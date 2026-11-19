using System.Globalization;
using System.Text;

namespace MessageFormat2.Functions.DateTimes;

// Builds .NET format patterns from the culture's own (DateTimeFormatInfo).
//
// The spec's options name sets of fields (month-day, year-month-day-weekday) and lengths. .NET only
// has a few fixed patterns per culture, so the rest are made by removing fields: in English the
// long date "dddd, MMMM d, yyyy" without the weekday is "MMMM d, yyyy", and without the year too,
// "MMMM d". A removed field takes one neighboring separator with it, and any suffix such as 年 in
// yyyy年.
//
// .NET has no medium date pattern, so medium is the long one with abbreviated month and weekday
// names ("MMM d, yyyy"). Output can differ from ICU's, which the test suite allows.
internal static class DateTimePatterns
{
    // the field letters of a .NET custom format string
    private const string FieldLetters = "dfFghHKmMstyz";

    public static string Format(DateTimeMoment moment, DateTimeOptions options, CultureInfo culture)
    {
        DateTimeFormatInfo info = culture.DateTimeFormat;

        string? date = options.Style == DateTimeStyle.Time
            ? null
            : Apply(moment.Local, DatePattern(options.DateFields, options.DateLength, info), culture);
        string? time = options.Style == DateTimeStyle.Date
            ? null
            : Apply(moment.Local, TimePattern(options.TimePrecision, options.Hour12, info), culture);

        string text = date is not null && time is not null ? date + DateTimeSeparator(info) + time : date ?? time!;

        if (time is not null && options.TimeZoneStyle is { } style && moment.Offset is { } offset)
        {
            text += " " + GmtOffset(offset, style);
        }

        return text;
    }

    private static string Apply(DateTime value, string pattern, CultureInfo culture) =>
        // A one-letter pattern such as "H" would be read as a standard format; "%H" is the custom field.
        value.ToString(pattern.Length == 1 ? "%" + pattern : pattern, culture);

    private static string DatePattern(DateFields fields, DateLength length, DateTimeFormatInfo info)
    {
        string longDate = info.LongDatePattern;
        string withWeekday = Tokenize(longDate).Any(IsWeekday) ? longDate : "dddd " + longDate;
        string withoutWeekday = Remove(longDate, IsWeekday);

        if (length == DateLength.Short && fields is DateFields.YearMonthDay or DateFields.MonthDay)
        {
            // The short pattern is all numbers, such as M/d/yyyy.
            return fields == DateFields.YearMonthDay ? info.ShortDatePattern : Remove(info.ShortDatePattern, IsYear);
        }

        string pattern = fields switch
        {
            DateFields.YearMonthDay => withoutWeekday,
            DateFields.YearMonthDayWeekday => withWeekday,
            DateFields.MonthDay => Remove(withoutWeekday, IsYear),
            DateFields.MonthDayWeekday => Remove(withWeekday, IsYear),
            DateFields.DayWeekday => Remove(withWeekday, token => IsYear(token) || token.Field == 'M'),
            _ => "dddd",
        };

        return length == DateLength.Long ? pattern : Abbreviate(pattern);
    }

    private static string TimePattern(TimePrecision precision, bool? hour12, DateTimeFormatInfo info)
    {
        string pattern = precision switch
        {
            TimePrecision.Second => info.LongTimePattern,
            TimePrecision.Minute => info.ShortTimePattern,
            _ => Remove(info.ShortTimePattern, token => token.Field == 'm'),
        };

        return hour12 switch
        {
            true => TwelveHour(pattern),
            false => Map(Remove(pattern, token => token.Field == 't'), token => token.Field is 'h' or 'H' ? "HH" : token.Text),
            null => pattern,
        };
    }

    // h is unpadded, as in ICU. Adds the AM/PM designator if the pattern has none. The 24-hour
    // clock uses HH, padded.
    private static string TwelveHour(string pattern)
    {
        string twelveHour = Map(pattern, token => token.Field == 'H' ? "h" : token.Text);
        return Tokenize(pattern).Any(token => token.Field == 't') ? twelveHour : twelveHour + " tt";
    }

    // Taken from the culture's full pattern; usually a space.
    private static string DateTimeSeparator(DateTimeFormatInfo info)
    {
        string full = info.FullDateTimePattern;
        if (full.StartsWith(info.LongDatePattern, StringComparison.Ordinal) && full.EndsWith(info.LongTimePattern, StringComparison.Ordinal))
        {
            int length = full.Length - info.LongDatePattern.Length - info.LongTimePattern.Length;
            string separator = length > 0 ? full.Substring(info.LongDatePattern.Length, length) : "";
            if (separator.Length > 0 && separator.All(c => !char.IsLetterOrDigit(c) && c is not ('\'' or '"' or '\\' or '%')))
            {
                return separator;
            }
        }

        return " ";
    }

    // GMT-7 (short) or GMT-07:00 (long), which is what ICU shows without zone names.
    private static string GmtOffset(TimeSpan offset, TimeZoneStyle style)
    {
        if (offset == TimeSpan.Zero)
        {
            return "GMT";
        }

        char sign = offset < TimeSpan.Zero ? '-' : '+';
        TimeSpan size = offset.Duration();
        return style == TimeZoneStyle.Short
            ? string.Create(CultureInfo.InvariantCulture, $"GMT{sign}{size.Hours}{(size.Minutes == 0 ? "" : $":{size.Minutes:00}")}")
            : string.Create(CultureInfo.InvariantCulture, $"GMT{sign}{size.Hours:00}:{size.Minutes:00}");
    }

    private enum TokenKind
    {
        Field,

        // letters right after a field that belong to it, such as 年 after yyyy
        Suffix,

        // spaces, punctuation, quoted text
        Separator,
    }

    private readonly record struct Token(string Text, TokenKind Kind, char Field);

    private static bool IsWeekday(Token token) => token.Field == 'd' && token.Text.Length >= 3;

    private static bool IsYear(Token token) => token.Field is 'y' or 'g';

    private static string Abbreviate(string pattern) =>
        Map(pattern, token => token.Field is 'M' or 'd' && token.Text.Length >= 4 ? token.Text[..3] : token.Text);

    private static string Map(string pattern, Func<Token, string> map) =>
        string.Concat(Tokenize(pattern).Select(token => token.Kind == TokenKind.Field ? map(token) : token.Text));

    // Each removed field takes its suffix and one neighboring separator with it.
    private static string Remove(string pattern, Func<Token, bool> remove)
    {
        List<Token> tokens = Tokenize(pattern);

        int index;
        while ((index = tokens.FindIndex(token => token.Kind == TokenKind.Field && remove(token))) >= 0)
        {
            int start = index;
            int end = index + 1;
            while (end < tokens.Count && tokens[end].Kind == TokenKind.Suffix)
            {
                end++;
            }

            bool separatorBefore = start > 0 && tokens[start - 1].Kind == TokenKind.Separator;
            bool separatorAfter = end < tokens.Count && tokens[end].Kind == TokenKind.Separator;

            if (separatorBefore && separatorAfter)
            {
                // Between two separators, drop the tighter one: in "h:mm tt", removing "mm" drops ":" and keeps " ".
                if (!HasSpace(tokens[start - 1]) && HasSpace(tokens[end]))
                {
                    start--;
                }
                else
                {
                    end++;
                }
            }
            else if (separatorBefore && end == tokens.Count)
            {
                start--;
            }
            else if (separatorAfter)
            {
                end++;
            }
            else if (separatorBefore)
            {
                start--;
            }

            tokens.RemoveRange(start, end - start);
        }

        int first = tokens.FindIndex(token => token.Kind != TokenKind.Separator);
        int last = tokens.FindLastIndex(token => token.Kind != TokenKind.Separator);
        return first < 0 ? "" : string.Concat(tokens.Skip(first).Take(last - first + 1).Select(token => token.Text));
    }

    private static bool HasSpace(Token token) => token.Text.Any(char.IsWhiteSpace);

    private static List<Token> Tokenize(string pattern)
    {
        var tokens = new List<Token>();
        var literal = new StringBuilder();

        int i = 0;
        while (i < pattern.Length)
        {
            char c = pattern[i];
            if (FieldLetters.Contains(c, StringComparison.Ordinal))
            {
                AddLiteral(tokens, literal);
                int start = i;
                while (i < pattern.Length && pattern[i] == c)
                {
                    i++;
                }

                tokens.Add(new Token(pattern[start..i], TokenKind.Field, c));
            }
            else if (c is '\'' or '"')
            {
                int close = pattern.IndexOf(c, i + 1);
                int end = close < 0 ? pattern.Length : close + 1;
                literal.Append(pattern, i, end - i);
                i = end;
            }
            else if (c == '\\' && i + 1 < pattern.Length)
            {
                literal.Append(pattern, i, 2);
                i += 2;
            }
            else
            {
                literal.Append(c);
                i++;
            }
        }

        AddLiteral(tokens, literal);
        return tokens;
    }

    // Letters directly after a field are its suffix, the rest is a separator.
    private static void AddLiteral(List<Token> tokens, StringBuilder literal)
    {
        if (literal.Length == 0)
        {
            return;
        }

        string text = literal.ToString();
        literal.Clear();

        int suffixLength = 0;
        if (tokens.Count > 0 && tokens[^1].Kind == TokenKind.Field)
        {
            while (suffixLength < text.Length && IsSuffixCharacter(text[suffixLength]))
            {
                suffixLength++;
            }
        }

        if (suffixLength > 0)
        {
            tokens.Add(new Token(text[..suffixLength], TokenKind.Suffix, '\0'));
        }

        if (suffixLength < text.Length)
        {
            tokens.Add(new Token(text[suffixLength..], TokenKind.Separator, '\0'));
        }
    }

    // non-ASCII letters and combining marks, such as 年, 일 or ที่
    private static bool IsSuffixCharacter(char c) =>
        !char.IsAscii(c) && (char.IsLetter(c) || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark);
}
