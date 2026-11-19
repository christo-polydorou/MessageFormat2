using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;

namespace MessageFormat2.Functions.Numbers;

// Currency symbols and fraction digits from .NET's culture data.
//
// .NET only describes each culture's own currency. A culture formatting its own currency uses its
// own symbol and digits. For any other currency they are gathered from every culture whose region
// uses it: the euro from de-DE, fr-FR and so on.
//
// The symbol display needs a symbol that isn't ambiguous. A symbol belongs to the currency that
// uses it in the most cultures, so $ is the US dollar and the Canadian dollar gets CA$. A borrowed
// symbol must also be readable outside its culture: any letters must be ASCII capitals (CA$, CHF),
// which rules out the Swedish kr or the Arabic symbol of the Kuwaiti dinar. A currency with no
// suitable symbol is shown by its code. narrowSymbol uses the most common symbol, whatever its
// owner or script.
//
// The table is built the first time a currency other than the culture's own is formatted. In
// invariant globalization mode every currency is shown by its code with two fraction digits, CLDR's
// default.
internal static class CurrencyData
{
    private const int DefaultFractionDigits = 2;

    private static readonly Lazy<FrozenDictionary<string, CurrencyInfo>> Table = new(Build);

    private static readonly ConcurrentDictionary<string, string?> OwnCurrencies = new(StringComparer.Ordinal);

    public static bool IsWellFormedCode(string code) => code.Length == 3 && code.All(char.IsAsciiLetter);

    public static int FractionDigits(string code, CultureInfo culture) =>
        IsOwnCurrency(code, culture)
            ? culture.NumberFormat.CurrencyDecimalDigits
            : Table.Value.TryGetValue(code, out CurrencyInfo? info) ? info.FractionDigits : DefaultFractionDigits;

    // null for CurrencyDisplay.Never
    public static string? Symbol(string code, CurrencyDisplay display, CultureInfo culture)
    {
        if (display is CurrencyDisplay.Never)
        {
            return null;
        }

        if (display is CurrencyDisplay.Code or CurrencyDisplay.Name)
        {
            return code;
        }

        if (IsOwnCurrency(code, culture))
        {
            return culture.NumberFormat.CurrencySymbol;
        }

        if (!Table.Value.TryGetValue(code, out CurrencyInfo? info))
        {
            return code;
        }

        return display is CurrencyDisplay.NarrowSymbol ? info.NarrowSymbol : info.Symbol;
    }

    private static bool IsOwnCurrency(string code, CultureInfo culture) =>
        string.Equals(OwnCurrencies.GetOrAdd(culture.Name, OwnCurrency), code, StringComparison.Ordinal);

    // null for neutral and invariant cultures
    private static string? OwnCurrency(string cultureName)
    {
        if (cultureName.Length == 0 || CultureInfo.GetCultureInfo(cultureName).IsNeutralCulture)
        {
            return null;
        }

        try
        {
            return new RegionInfo(cultureName).ISOCurrencySymbol;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static FrozenDictionary<string, CurrencyInfo> Build()
    {
        var symbols = new Dictionary<(string Code, string Symbol), int>();
        var digits = new Dictionary<(string Code, int Digits), int>();

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            if (OwnCurrency(culture.Name) is not { } code || !IsWellFormedCode(code))
            {
                continue;
            }

            NumberFormatInfo format = culture.NumberFormat;
            Increment(symbols, (code, format.CurrencySymbol));
            Increment(digits, (code, format.CurrencyDecimalDigits));
        }

        // Each symbol belongs to the currency that uses it most; a tie means it belongs to none.
        var owners = symbols
            .GroupBy(entry => entry.Key.Symbol, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(entry => entry.Value).ToList())
            .Where(users => users.Count == 1 || users[0].Value > users[1].Value)
            .ToDictionary(users => users[0].Key.Symbol, users => users[0].Key.Code, StringComparer.Ordinal);

        return symbols
            .GroupBy(entry => entry.Key.Code, StringComparer.Ordinal)
            .ToFrozenDictionary(
                group => group.Key,
                group =>
                {
                    string code = group.Key;
                    List<string> bySymbolCount = group
                        .OrderByDescending(entry => entry.Value)
                        .ThenBy(entry => entry.Key.Symbol, StringComparer.Ordinal)
                        .Select(entry => entry.Key.Symbol)
                        .ToList();
                    string symbol = bySymbolCount.FirstOrDefault(
                        candidate => owners.GetValueOrDefault(candidate) == code && IsInternational(candidate)) ?? code;
                    int fractionDigits = digits
                        .Where(entry => entry.Key.Code == code)
                        .MaxBy(entry => entry.Value).Key.Digits;
                    return new CurrencyInfo(symbol, bySymbolCount[0], fractionDigits);
                },
                StringComparer.Ordinal);
    }

    // readable in any language: no letters other than ASCII capitals
    private static bool IsInternational(string symbol) =>
        symbol.All(c => !char.IsLetter(c) || char.IsAsciiLetterUpper(c)) && !symbol.Any(char.IsControl) && !symbol.Any(IsFormatCharacter);

    private static bool IsFormatCharacter(char c) => char.GetUnicodeCategory(c) == UnicodeCategory.Format;

    private static void Increment<TKey>(Dictionary<TKey, int> counts, TKey key)
        where TKey : notnull =>
        counts[key] = counts.GetValueOrDefault(key) + 1;

    private sealed record CurrencyInfo(string Symbol, string NarrowSymbol, int FractionDigits);
}
