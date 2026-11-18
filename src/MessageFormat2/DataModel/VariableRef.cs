namespace MessageFormat2.DataModel;

/// <summary>A reference to a variable, such as <c>$count</c>.</summary>
public sealed class VariableRef : Operand
{
    /// <summary>Creates a variable reference.</summary>
    /// <param name="name">The variable name, without the leading <c>$</c>.</param>
    public VariableRef(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    /// <summary>The variable name, without the leading <c>$</c>.</summary>
    public string Name { get; }
}
