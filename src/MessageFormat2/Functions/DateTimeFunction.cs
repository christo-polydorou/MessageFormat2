using MessageFormat2.Functions.DateTimes;

namespace MessageFormat2.Functions;

// :date, :time and :datetime (spec/functions/datetime.md, still Draft).
// A value is shown in its own UTC offset, or in the zone named by timeZone. A value with no offset
// (a "floating" time) is shown as is. Not usable as selectors.
internal sealed class DateTimeFunction : MessageFunction
{
    private readonly DateTimeStyle _style;
    private readonly string _name;

    private DateTimeFunction(DateTimeStyle style, string name)
    {
        _style = style;
        _name = name;
    }

    public static DateTimeFunction Date { get; } = new(DateTimeStyle.Date, ":date");

    public static DateTimeFunction Time { get; } = new(DateTimeStyle.Time, ":time");

    public static DateTimeFunction DateTime { get; } = new(DateTimeStyle.DateTime, ":datetime");

    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        (DateTimeMoment moment, DateTimeOptions? inherited) = ReadOperand(operand);
        if (!moment.HasDate && _style != DateTimeStyle.Time)
        {
            throw new MessageFunctionException(MessageErrorType.BadOperand, $"{_name} requires a date; a time of day has none.");
        }

        DateTimeOptions resolved = DateTimeOptionParser.Parse(DateTimeOptions.For(_style, inherited), options, context, out bool useInputZone);

        if (useInputZone)
        {
            if (moment.Offset is null)
            {
                context.ReportError(MessageErrorType.BadOperand, "timeZone=input requires an operand with a time zone or offset.");
            }
        }
        else if (resolved.TimeZone is { } zone)
        {
            moment = moment.InZone(zone);
        }

        return new DateTimeValue(moment, resolved, context.Culture);
    }

    private (DateTimeMoment Moment, DateTimeOptions? Options) ReadOperand(MessageValue? operand)
    {
        if (operand is DateTimeValue value)
        {
            return (value.Moment, value.Options);
        }

        if (operand?.Value is string text && DateTimeLiteral.TryParse(text, out DateTimeMoment parsed))
        {
            return (parsed, null);
        }

        if (DateTimeMoment.TryFrom(operand?.Value, out DateTimeMoment moment))
        {
            return (moment, null);
        }

        throw new MessageFunctionException(
            MessageErrorType.BadOperand, $"{_name} requires a date or time, or an ISO 8601 string such as 2006-01-02T15:04:06.");
    }
}
