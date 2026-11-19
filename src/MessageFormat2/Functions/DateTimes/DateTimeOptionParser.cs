namespace MessageFormat2.Functions.DateTimes;

// Invalid values are reported as Bad Option and ignored. Everything except timeZone, hour12 and
// calendar must be a literal. calendar is accepted but ignored: only the culture's default calendar
// is supported.
internal static class DateTimeOptionParser
{
    public static DateTimeOptions Parse(
        DateTimeOptions result, MessageOptionDictionary options, MessageFunctionContext context, out bool useInputZone)
    {
        useInputZone = false;
        bool hasTime = result.Style != DateTimeStyle.Date;

        foreach ((string name, MessageValue value) in options)
        {
            switch (result.Style, name)
            {
                case (DateTimeStyle.Date, "fields") or (DateTimeStyle.DateTime, "dateFields"):
                    result = Literal(context, options, name, value, result, DateFieldValues, (r, v) => r with { DateFields = v });
                    break;
                case (DateTimeStyle.Date, "length") or (DateTimeStyle.DateTime, "dateLength"):
                    result = Literal(context, options, name, value, result, DateLengths, (r, v) => r with { DateLength = v });
                    break;
                case (DateTimeStyle.Time, "precision") or (DateTimeStyle.DateTime, "timePrecision"):
                    result = Literal(context, options, name, value, result, TimePrecisions, (r, v) => r with { TimePrecision = v });
                    break;
                case (_, "timeZoneStyle") when hasTime:
                    result = Literal(context, options, name, value, result, TimeZoneStyles, (r, v) => r with { TimeZoneStyle = v });
                    break;
                case (_, "hour12") when hasTime:
                    result = value.Value switch
                    {
                        true or "true" => result with { Hour12 = true },
                        false or "false" => result with { Hour12 = false },
                        _ => Invalid(context, name, value, result),
                    };
                    break;
                case (_, "timeZone"):
                    if (value.Value is TimeZoneOption.Input)
                    {
                        useInputZone = true;
                        result = result with { TimeZone = null };
                    }
                    else if (value.Value is string zoneName && TimeZoneOption.Find(zoneName) is { } zone)
                    {
                        result = result with { TimeZone = zone };
                    }
                    else
                    {
                        result = Invalid(context, name, value, result);
                    }

                    break;
            }
        }

        return result;
    }

    private static DateTimeOptions Literal<T>(
        MessageFunctionContext context, MessageOptionDictionary options, string name, MessageValue value, DateTimeOptions result,
        Dictionary<string, T> allowed, Func<DateTimeOptions, T, DateTimeOptions> apply)
    {
        if (!options.IsLiteral(name))
        {
            context.ReportError(MessageErrorType.BadOption, $"The {name} option must be a literal.");
            return result;
        }

        return value.Value is string text && allowed.TryGetValue(text, out T? parsed)
            ? apply(result, parsed)
            : Invalid(context, name, value, result);
    }

    private static DateTimeOptions Invalid(MessageFunctionContext context, string name, MessageValue value, DateTimeOptions result)
    {
        context.ReportError(MessageErrorType.BadOption, $"'{value.Value}' is not a valid value for {name}.");
        return result;
    }

    private static readonly Dictionary<string, DateFields> DateFieldValues = new(StringComparer.Ordinal)
    {
        ["weekday"] = DateFields.Weekday,
        ["day-weekday"] = DateFields.DayWeekday,
        ["month-day"] = DateFields.MonthDay,
        ["month-day-weekday"] = DateFields.MonthDayWeekday,
        ["year-month-day"] = DateFields.YearMonthDay,
        ["year-month-day-weekday"] = DateFields.YearMonthDayWeekday,
    };

    private static readonly Dictionary<string, DateLength> DateLengths = new(StringComparer.Ordinal)
    {
        ["long"] = DateLength.Long,
        ["medium"] = DateLength.Medium,
        ["short"] = DateLength.Short,
    };

    private static readonly Dictionary<string, TimePrecision> TimePrecisions = new(StringComparer.Ordinal)
    {
        ["hour"] = TimePrecision.Hour,
        ["minute"] = TimePrecision.Minute,
        ["second"] = TimePrecision.Second,
    };

    private static readonly Dictionary<string, TimeZoneStyle> TimeZoneStyles = new(StringComparer.Ordinal)
    {
        ["long"] = TimeZoneStyle.Long,
        ["short"] = TimeZoneStyle.Short,
    };
}
