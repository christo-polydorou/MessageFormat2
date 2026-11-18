using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>A message that chooses one of several variants based on its selectors.</summary>
/// <example>
/// <code>
/// .input {$count :number}
/// .match $count
/// one {{You have one item.}}
/// *   {{You have {$count} items.}}
/// </code>
/// </example>
public sealed class SelectMessage : Message
{
    /// <summary>Creates a select message.</summary>
    public SelectMessage(
        IEnumerable<Declaration> declarations,
        IEnumerable<VariableRef> selectors,
        IEnumerable<Variant> variants)
        : base(declarations)
    {
        Selectors = ReadOnly.Copy(selectors);
        Variants = ReadOnly.Copy(variants);
    }

    /// <summary>The variables named after <c>.match</c>, in source order.</summary>
    public IReadOnlyList<VariableRef> Selectors { get; }

    /// <summary>The variants, in source order.</summary>
    public IReadOnlyList<Variant> Variants { get; }
}
