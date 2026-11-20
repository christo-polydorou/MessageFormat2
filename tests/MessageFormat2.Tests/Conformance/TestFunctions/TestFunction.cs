using System.Globalization;

using MessageFormat2.Functions;

namespace MessageFormat2.Tests.Conformance.TestFunctions;

// :test:function, :test:select and :test:format, from the "Test Functions" section of the test
// suite's README. The three share one behavior; they differ only in whether formatting and
// selection are allowed.
public sealed class TestFunction : MessageFunction
{
    private readonly bool _canFormat;
    private readonly bool _canSelect;

    private TestFunction(bool canFormat, bool canSelect)
    {
        _canFormat = canFormat;
        _canSelect = canSelect;
    }

    public static IReadOnlyDictionary<string, MessageFunction> All { get; } = new Dictionary<string, MessageFunction>
    {
        ["test:function"] = new TestFunction(canFormat: true, canSelect: true),
        ["test:select"] = new TestFunction(canFormat: false, canSelect: true),
        ["test:format"] = new TestFunction(canFormat: true, canSelect: false),
    };

    public override MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options)
    {
        int decimalPlaces = 0;
        bool failsFormat = false;
        bool failsSelect = false;
        double input;

        if (operand is TestValue previous)
        {
            input = previous.Input;
            decimalPlaces = previous.DecimalPlaces;
            failsFormat = previous.FailsFormat;
            failsSelect = previous.FailsSelect;
        }
        else if (!TryGetNumber(operand?.Value, out input))
        {
            throw new MessageFunctionException(MessageErrorType.BadOperand, "The operand must be a number.");
        }

        if (options.TryGetValue("decimalPlaces", out MessageValue? decimalPlacesOption))
        {
            decimalPlaces = decimalPlacesOption.Value switch
            {
                0 or 0L or 0.0 or 0m or "0" => 0,
                1 or 1L or 1.0 or 1m or "1" => 1,
                _ => throw new MessageFunctionException(MessageErrorType.BadOption, "decimalPlaces must be 0 or 1."),
            };
        }

        if (options.TryGetValue("fails", out MessageValue? failsOption))
        {
            switch (failsOption.Value)
            {
                case "always":
                    failsFormat = true;
                    failsSelect = true;
                    break;
                case "format":
                    failsFormat = true;
                    break;
                case "select":
                    failsSelect = true;
                    break;
                case "never":
                    break;
                default:
                    context.ReportError(MessageErrorType.BadOption, "fails must be never, select, format, or always.");
                    break;
            }
        }

        return new TestValue(input, decimalPlaces, failsFormat, failsSelect, _canFormat, _canSelect);
    }

    private static bool TryGetNumber(object? value, out double number)
    {
        switch (value)
        {
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case double d:
                number = d;
                return true;
            case decimal m:
                number = (double)m;
                return true;
            case string s:
                return MessageFormat2.Functions.NumberLiteral.TryParse(s, out number);
            default:
                number = 0;
                return false;
        }
    }

    private sealed class TestValue(double input, int decimalPlaces, bool failsFormat, bool failsSelect, bool canFormat, bool canSelect)
        : MessageValue
    {
        public double Input { get; } = input;

        public int DecimalPlaces { get; } = decimalPlaces;

        public bool FailsFormat { get; } = failsFormat;

        public bool FailsSelect { get; } = failsSelect;

        public override string Type => "test";

        // When used as an option value, the resolved value is the input.
        public override object? Value => Input;

        public override string FormatToString()
        {
            if (!canFormat)
            {
                throw new MessageFunctionException(MessageErrorType.UnsupportedOperation, "Not formattable.");
            }

            if (FailsFormat)
            {
                throw new MessageFunctionException(MessageErrorType.BadOption, "Formatting failed as requested by the fails option.");
            }

            double absolute = Math.Abs(Input);
            double integer = Math.Floor(absolute);
            string text = (Input < 0 ? "-" : "") + integer.ToString("F0", CultureInfo.InvariantCulture);

            if (DecimalPlaces == 1)
            {
                text += "." + Math.Floor((absolute - integer) * 10).ToString("F0", CultureInfo.InvariantCulture);
            }

            return text;
        }

        public override bool SupportsSelection => canSelect;

        public override bool Matches(string key)
        {
            if (FailsSelect)
            {
                throw new MessageFunctionException(MessageErrorType.BadOption, "Selection failed as requested by the fails option.");
            }

            return Input == 1 && (key == "1" || (DecimalPlaces == 1 && key == "1.0"));
        }

        public override bool IsBetterMatch(string key1, string key2) => key1 == "1.0";
    }
}
