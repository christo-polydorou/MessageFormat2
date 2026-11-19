using System.Globalization;

using MessageFormat2.Functions.DateTimes;

namespace MessageFormat2.Functions;

/// <summary>
/// A date, time, or both, with its formatting options: the result of <c>:date</c>, <c>:time</c>
/// or <c>:datetime</c>, and of any .NET date or time argument used without a function.
/// </summary>
/// <remarks>Date/time values cannot be used as selectors.</remarks>
public sealed class DateTimeValue : MessageValue
{
    private readonly CultureInfo _culture;

    internal DateTimeValue(DateTimeMoment moment, DateTimeOptions options, CultureInfo culture)
    {
        Moment = moment;
        Options = options;
        _culture = culture;
    }

    internal DateTimeMoment Moment { get; }

    internal DateTimeOptions Options { get; }

    /// <inheritdoc/>
    public override string Type => "datetime";

    /// <summary>
    /// The value: a <see cref="DateTimeOffset"/> if its offset is known, a <see cref="DateTime"/>
    /// with <see cref="DateTimeKind.Unspecified"/> if not, or a <see cref="TimeOnly"/> for a time of day.
    /// </summary>
    public override object? Value => Moment.ToClrValue();

    /// <inheritdoc/>
    public override string? Locale => _culture.Name;

    /// <inheritdoc/>
    public override TextDirection Direction =>
        _culture.TextInfo.IsRightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;

    /// <inheritdoc/>
    public override string FormatToString() => DateTimePatterns.Format(Moment, Options, _culture);

    internal static DateTimeValue? FromArgument(object? argument, CultureInfo culture)
    {
        if (!DateTimeMoment.TryFrom(argument, out DateTimeMoment moment))
        {
            return null;
        }

        DateTimeStyle style = argument switch
        {
            DateOnly => DateTimeStyle.Date,
            TimeOnly => DateTimeStyle.Time,
            _ => DateTimeStyle.DateTime,
        };
        return new DateTimeValue(moment, DateTimeOptions.For(style), culture);
    }
}
