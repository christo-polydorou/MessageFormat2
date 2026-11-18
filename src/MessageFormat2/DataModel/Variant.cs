using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>One branch of a <see cref="SelectMessage"/>: a list of keys and the pattern to use when they match.</summary>
public sealed class Variant
{
    /// <summary>Creates a variant.</summary>
    public Variant(IEnumerable<VariantKey> keys, Pattern value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Keys = ReadOnly.Copy(keys);
        Value = value;
    }

    /// <summary>The keys, one per selector.</summary>
    public IReadOnlyList<VariantKey> Keys { get; }

    /// <summary>The pattern formatted when this variant is selected.</summary>
    public Pattern Value { get; }
}
