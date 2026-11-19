namespace MessageFormat2.Functions.DateTimes;

internal enum DateTimeStyle
{
    Date,
    Time,
    DateTime,
}

// fields (:date) and dateFields (:datetime)
internal enum DateFields
{
    Weekday,
    DayWeekday,
    MonthDay,
    MonthDayWeekday,
    YearMonthDay,
    YearMonthDayWeekday,
}

// length (:date) and dateLength (:datetime)
internal enum DateLength
{
    Long,
    Medium,
    Short,
}

// precision (:time) and timePrecision (:datetime)
internal enum TimePrecision
{
    Hour,
    Minute,
    Second,
}

internal enum TimeZoneStyle
{
    // GMT-07:00
    Long,

    // GMT-7
    Short,
}

// Only timeZone and hour12 carry over from one expression to the next: {$d :time} where $d has
// timeZone=UTC keeps UTC, but not $d's dateLength.
internal sealed record DateTimeOptions
{
    public DateTimeStyle Style { get; init; }

    public DateFields DateFields { get; init; } = DateFields.YearMonthDay;

    public DateLength DateLength { get; init; } = DateLength.Medium;

    public TimePrecision TimePrecision { get; init; } = TimePrecision.Minute;

    // null: no time zone shown
    public TimeZoneStyle? TimeZoneStyle { get; init; }

    // null: the culture's clock
    public bool? Hour12 { get; init; }

    // null: the value's own offset
    public TimeZoneInfo? TimeZone { get; init; }

    public static DateTimeOptions For(DateTimeStyle style, DateTimeOptions? inherited = null) => new()
    {
        Style = style,
        Hour12 = inherited?.Hour12,
        TimeZone = inherited?.TimeZone,
    };
}
