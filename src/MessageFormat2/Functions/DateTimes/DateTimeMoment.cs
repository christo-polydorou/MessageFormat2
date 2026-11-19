namespace MessageFormat2.Functions.DateTimes;

// A wall-clock date and time plus its UTC offset, if known.
// Local always has Kind Unspecified. Offset is null for a "floating" time with no zone. HasDate is
// false for a TimeOnly.
internal readonly record struct DateTimeMoment(DateTime Local, TimeSpan? Offset, bool HasDate)
{
    // A DateTime with Kind Utc or Local has an offset; Unspecified is floating. A DateOnly is
    // midnight, as the spec says for a date without a time.
    public static bool TryFrom(object? value, out DateTimeMoment moment)
    {
        moment = value switch
        {
            DateTimeOffset offset => new(Unspecified(offset.DateTime), offset.Offset, true),
            DateTime { Kind: DateTimeKind.Utc } utc => new(Unspecified(utc), TimeSpan.Zero, true),
            DateTime { Kind: DateTimeKind.Local } local => new(Unspecified(local), TimeZoneInfo.Local.GetUtcOffset(local), true),
            DateTime floating => new(floating, null, true),
            DateOnly date => new(date.ToDateTime(TimeOnly.MinValue), null, true),
            TimeOnly time => new(DateTime.MinValue.Add(time.ToTimeSpan()), null, false),
            _ => default,
        };

        return value is DateTimeOffset or DateTime or DateOnly or TimeOnly;
    }

    public object ToClrValue() =>
        !HasDate ? TimeOnly.FromDateTime(Local)
        : Offset is { } offset ? new DateTimeOffset(Local, offset)
        : Local;

    // A floating time is taken to be in the zone already (15:04 stays 15:04 and gains the zone's
    // offset); a time with an offset is converted.
    public DateTimeMoment InZone(TimeZoneInfo zone)
    {
        if (Offset is not { } offset)
        {
            return this with { Offset = zone.GetUtcOffset(Local) };
        }

        // Only values with a date have an offset (a TimeOnly is always floating), so conversion is well defined.
        DateTimeOffset converted = TimeZoneInfo.ConvertTime(new DateTimeOffset(Local, offset), zone);
        return new DateTimeMoment(converted.DateTime, converted.Offset, true);
    }

    private static DateTime Unspecified(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
}
