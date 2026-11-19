using System.Globalization;

namespace MessageFormat2;

/// <summary>
/// An amount of money in a given currency, such as 42.50 EUR. Used without a function it formats
/// as <c>:currency</c>, and a message cannot change its currency.
/// </summary>
public readonly record struct CurrencyAmount
{
    /// <summary>Creates an amount.</summary>
    /// <param name="amount">The amount.</param>
    /// <param name="currency">An ISO 4217 currency code: three letters, such as <c>"EUR"</c>, in any case.</param>
    /// <exception cref="ArgumentException"><paramref name="currency"/> is not three ASCII letters.</exception>
    public CurrencyAmount(decimal amount, string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("A currency code is three letters, such as EUR.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>The amount.</summary>
    public decimal Amount { get; }

    /// <summary>The currency code, in upper case. Null only for <c>default(CurrencyAmount)</c>.</summary>
    public string Currency { get; }

    /// <summary>The amount and code, such as <c>"42.50 EUR"</c>, independent of culture.</summary>
    public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";
}
