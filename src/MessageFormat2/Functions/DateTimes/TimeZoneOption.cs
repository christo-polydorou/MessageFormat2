using System.Globalization;
using System.Text.RegularExpressions;

namespace MessageFormat2.Functions.DateTimes;

internal static partial class TimeZoneOption
{
    public const string Input = "input";

    // Accepts UTC, an IANA name such as Europe/Paris, or an offset such as +05:30. Returns null for
    // anything else, or for a zone this system doesn't know.
    public static TimeZoneInfo? Find(string value)
    {
        if (value == "UTC")
        {
            return TimeZoneInfo.Utc;
        }

        Match offset = Offset().Match(value);
        if (offset.Success)
        {
            var span = new TimeSpan(
                int.Parse(offset.Groups["hours"].Value, CultureInfo.InvariantCulture),
                int.Parse(offset.Groups["minutes"].Value, CultureInfo.InvariantCulture),
                0);
            if (offset.Groups["sign"].Value == "-")
            {
                span = -span;
            }

            // DateTimeOffset allows at most ±14 hours.
            return span.Duration() <= TimeSpan.FromHours(14)
                ? TimeZoneInfo.CreateCustomTimeZone(value, span, value, value)
                : null;
        }

        return ZoneName().IsMatch(value) && TimeZoneInfo.TryFindSystemTimeZoneById(value, out TimeZoneInfo? zone) ? zone : null;
    }

    // RFC 3339 time-numoffset
    [GeneratedRegex(@"^(?<sign>[+-])(?<hours>[01][0-9]|2[0-3]):(?<minutes>[0-5][0-9])$", RegexOptions.CultureInvariant)]
    private static partial Regex Offset();

    // RFC 9557 time-zone-name
    [GeneratedRegex(@"^[A-Za-z._][A-Za-z._0-9+-]*(/[A-Za-z._][A-Za-z._0-9+-]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ZoneName();
}
