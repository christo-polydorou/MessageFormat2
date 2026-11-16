using System.Text.Json.Nodes;

using MessageFormat2.DataModel;
using MessageFormat2.Formatting;
using MessageFormat2.Functions;
using MessageFormat2.Syntax;
using MessageFormat2.Tests.Conformance.TestFunctions;
using MessageFormat2.Validation;

namespace MessageFormat2.Tests.Conformance;

// Runs every case in the official test suite as its own test. Each case is checked in stages:
// parsing, then validation, then formatting. Cases that aren't expected to pass are listed in
// KnownSkips.
public sealed class ConformanceTests
{
    public static TheoryData<string, int> AllCases()
    {
        var data = new TheoryData<string, int>();
        foreach (string file in ConformanceSuite.FileNames)
        {
            foreach (ConformanceTestCase testCase in ConformanceSuite.Cases(file))
            {
                data.Add(new TheoryDataRow<string, int>(file, testCase.Index)
                {
                    TestDisplayName = testCase.ToString(),
                });
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void Case(string file, int index)
    {
        ConformanceTestCase testCase = ConformanceSuite.Cases(file)[index];

        if (KnownSkips.Reason(testCase) is { } reason)
        {
            Assert.Skip(reason);
        }

        if (KnownSkips.PendingFeature(testCase) is not { } pending)
        {
            Run(testCase);
            return;
        }

        try
        {
            Run(testCase);
        }
        catch (Exception exception) when (exception is Xunit.Sdk.IAssertionException)
        {
            Assert.Skip($"{pending}. Currently fails: {exception.Message.Split('\n')[0]}");
        }
    }

    private static void Run(ConformanceTestCase testCase)
    {
        IReadOnlyList<string> expectedErrors = testCase.ExpErrors ?? [];

        Message message;
        try
        {
            message = MessageParser.Parse(testCase.Src);
        }
        catch (MessageFormatException exception) when (exception.ErrorType == MessageErrorType.SyntaxError)
        {
            Assert.True(
                expectedErrors.Contains("syntax-error"),
                $"Unexpected syntax error: {exception.Message}");
            return;
        }

        Assert.False(expectedErrors.Contains("syntax-error"), "Expected a syntax error, but the message parsed.");

        string[] actualDataModelErrors = MessageValidator.GetErrors(message)
            .Select(error => ErrorTypeNames.ToTestName(error.Type))
            .ToArray();
        string[] expectedDataModelErrors = expectedErrors.Where(ErrorTypeNames.IsDataModelError).ToArray();

        Assert.Equal(expectedDataModelErrors, actualDataModelErrors);
        if (actualDataModelErrors.Length > 0)
        {
            return;
        }

        var options = new MessageFormatterOptions
        {
            BidiIsolation = testCase.BidiIsolation == "none" ? BidiIsolation.None : BidiIsolation.Default,
        };
        foreach ((string name, MessageFunction function) in TestFunction.All)
        {
            options.Functions[name] = function;
        }

        var formatter = new MessageFormatter(message, ConformanceArguments.Culture(testCase.Locale), options);
        Dictionary<string, object?> arguments = ConformanceArguments.From(testCase.Params);

        string text = formatter.Format(arguments, out IReadOnlyList<MessageError> errors);
        IReadOnlyList<MessagePart> parts = formatter.FormatToParts(arguments);

        if (testCase.Exp is not null)
        {
            Assert.Equal(testCase.Exp, text);
        }

        if (testCase.ExpParts is { } expectedParts)
        {
            var actualParts = new JsonArray(parts.Select(part => (JsonNode)PartDescriptions.Describe(part)).ToArray());
            Assert.True(
                PartDescriptions.Contains(actualParts, expectedParts),
                $"Expected parts {expectedParts.GetRawText()}{Environment.NewLine}Actual parts   {actualParts.ToJsonString()}");
        }

        Assert.Equal(expectedErrors, errors.Select(error => ErrorTypeNames.ToTestName(error.Type)));
    }
}
