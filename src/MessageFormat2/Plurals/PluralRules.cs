using System.Globalization;

namespace MessageFormat2.Plurals;

// The rules themselves are generated into PluralRules.g.cs by tools/PluralRuleGenerator.
internal static partial class PluralRules
{
    public static PluralCategory SelectCardinal(CultureInfo culture, in PluralOperands operands) =>
        Select(culture, operands, Cardinal);

    public static PluralCategory SelectOrdinal(CultureInfo culture, in PluralOperands operands) =>
        Select(culture, operands, Ordinal);

    public static string ToKey(PluralCategory category) => category switch
    {
        PluralCategory.Zero => "zero",
        PluralCategory.One => "one",
        PluralCategory.Two => "two",
        PluralCategory.Few => "few",
        PluralCategory.Many => "many",
        _ => "other",
    };

    private delegate PluralCategory? RuleSet(string language, in PluralOperands operands);

    // Tries the culture name and then each shorter prefix (sr-Latn-RS, sr-Latn, sr). Languages
    // without rules, and the invariant culture, use "other" for every number.
    private static PluralCategory Select(CultureInfo culture, in PluralOperands operands, RuleSet rules)
    {
        string name = culture.Name;
        while (name.Length > 0)
        {
            if (rules(name, operands) is { } category)
            {
                return category;
            }

            int dash = name.LastIndexOf('-');
            name = dash < 0 ? "" : name[..dash];
        }

        return PluralCategory.Other;
    }

    // CLDR's range relation x = a..b: true only for integers from a to b.
    private static bool InRange(decimal value, decimal low, decimal high) =>
        value == decimal.Truncate(value) && value >= low && value <= high;
}
