using System.Globalization;
using System.Text.RegularExpressions;

namespace MessageFormat2.Functions.DateTimes;

// An ISO 8601 date or date-time with an optional UTC offset: 2006-01-02, 2006-01-02T15:04:06 or
// 2006-01-02T15:04:06.5+02:00. The regex is the one in spec/functions/datetime.md, "Date and Time
// Operands".
internal static partial class DateTimeLiteral
{
    public static bool TryParse(string text, out DateTimeMoment moment)
    {
        moment = default;
        Match match = Pattern().Match(text);
        if (!match.Success)
        {
            return false;
        }

        int Group(string name) => match.Groups[name].Success ? int.Parse(match.Groups[name].Value, CultureInfo.InvariantCulture) : 0;

        int year = Group("year");
        int month = Group("month");
        int day = Group("day");
        if (day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        // Fraction digits are milliseconds: ".5" is 500 ms.
        int milliseconds = match.Groups["fraction"].Success
            ? int.Parse(match.Groups["fraction"].Value.PadRight(3, '0'), CultureInfo.InvariantCulture)
            : 0;
        var local = new DateTime(year, month, day, Group("hour"), Group("minute"), Group("second"), milliseconds, DateTimeKind.Unspecified);

        TimeSpan? offset = match.Groups["zulu"].Success
            ? TimeSpan.Zero
            : match.Groups["offsetHours"].Success
                ? (match.Groups["sign"].Value == "-" ? -1 : 1) * new TimeSpan(Group("offsetHours"), Group("offsetMinutes"), 0)
                : null;

        moment = new DateTimeMoment(local, offset, true);
        return true;
    }

    [GeneratedRegex(
        @"^(?!0000)(?<year>[0-9]{4})-(?<month>0[1-9]|1[0-2])-(?<day>0[1-9]|[12][0-9]|3[01])" +
        @"(T(?<hour>[01][0-9]|2[0-3]):(?<minute>[0-5][0-9]):(?<second>[0-5][0-9])(\.(?<fraction>[0-9]{1,3}))?" +
        @"((?<zulu>Z)|(?<sign>[+-])(?<offsetHours>0[0-9]|1[0-3]|14(?=:00)):(?<offsetMinutes>[0-5][0-9]))?)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
