using System.Globalization;
using System.Text;

namespace PluralRuleGenerator;

// Translates one CLDR plural rule condition into a C# boolean expression over a PluralOperands
// value named o.
//
// Grammar (TR35, "Language Plural Rules"):
//   condition     = and_condition ('or' and_condition)*
//   and_condition = relation ('and' relation)*
//   relation      = expr ('=' | '!=') range_list
//   expr          = operand (('mod' | '%') value)?
//   range_list    = (range | value) (',' range_list)*
//   range         = value'..'value
internal static class RuleTranslator
{
    // e.g. "i = 1 and v = 0 @integer 1". Samples after @ are ignored.
    public static string Translate(string rule)
    {
        string condition = rule.Split('@')[0].Trim();
        if (condition.Length == 0)
        {
            return "true";
        }

        IEnumerable<string> alternatives = Split(condition, "or").Select(and =>
            string.Join(" && ", Split(and, "and").Select(TranslateRelation)));

        return string.Join(" || ", alternatives.Select(alternative => alternative.Contains("&&") ? $"({alternative})" : alternative));
    }

    private static string[] Split(string text, string keyword) =>
        text.Split($" {keyword} ", StringSplitOptions.TrimEntries);

    private static string TranslateRelation(string relation)
    {
        bool negated = relation.Contains("!=", StringComparison.Ordinal);
        string[] sides = relation.Split(negated ? "!=" : "=", StringSplitOptions.TrimEntries);

        string expression = TranslateExpression(sides[0]);
        IEnumerable<string> tests = sides[1].Split(',', StringSplitOptions.TrimEntries).Select(item =>
        {
            string[] range = item.Split("..");
            return range.Length == 1
                ? $"{expression} == {range[0]}"
                : $"InRange({expression}, {range[0]}, {range[1]})";
        });

        string test = string.Join(" || ", tests);
        bool several = sides[1].Contains(',');

        return (negated, several) switch
        {
            (false, false) => test,
            (false, true) => $"({test})",
            (true, _) when !several && !test.StartsWith("InRange", StringComparison.Ordinal) => test.Replace(" == ", " != ", StringComparison.Ordinal),
            (true, _) => $"!({test})",
        };
    }

    private static string TranslateExpression(string expression)
    {
        string[] parts = expression.Replace(" mod ", " % ", StringComparison.Ordinal).Split('%', StringSplitOptions.TrimEntries);
        string operand = parts[0] switch
        {
            "n" => "o.N",
            "i" => "o.I",
            "v" => "o.V",
            "w" => "o.W",
            "f" => "o.F",
            "t" => "o.T",
            "c" or "e" => "o.E",
            _ => throw new FormatException($"Unknown plural operand '{parts[0]}'."),
        };

        return parts.Length == 1 ? operand : $"{operand} % {int.Parse(parts[1], CultureInfo.InvariantCulture)}";
    }
}
