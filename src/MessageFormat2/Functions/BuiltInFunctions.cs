namespace MessageFormat2.Functions;

internal static class BuiltInFunctions
{
    public static IEnumerable<KeyValuePair<string, MessageFunction>> All { get; } =
    [
        new("string", new StringFunction()),
        new("number", NumberFunction.Number),
        new("integer", NumberFunction.Integer),
        new("offset", new OffsetFunction()),
        new("percent", new PercentFunction()),
        new("currency", new CurrencyFunction()),
        new("date", DateTimeFunction.Date),
        new("time", DateTimeFunction.Time),
        new("datetime", DateTimeFunction.DateTime),
    ];
}
