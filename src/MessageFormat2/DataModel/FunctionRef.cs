using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>A function annotation with its options, such as <c>:number minimumFractionDigits=2</c>.</summary>
public sealed class FunctionRef
{
    /// <summary>Creates a function reference.</summary>
    /// <param name="name">The function identifier, without the leading <c>:</c>. May include a namespace, as in <c>ns:name</c>.</param>
    /// <param name="options">The options, in source order.</param>
    public FunctionRef(string name, IEnumerable<MessageOption>? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        Options = ReadOnly.Copy(options ?? []);
    }

    /// <summary>The function identifier, without the leading <c>:</c>.</summary>
    public string Name { get; }

    /// <summary>The options, in source order.</summary>
    public IReadOnlyList<MessageOption> Options { get; }
}
