namespace MessageFormat2.DataModel;

/// <summary>The catch-all variant key <c>*</c>, which matches any value.</summary>
public sealed class CatchallKey : VariantKey
{
    /// <summary>The single catch-all key.</summary>
    public static CatchallKey Instance { get; } = new();

    private CatchallKey()
    {
    }
}
