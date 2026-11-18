namespace MessageFormat2.DataModel;

/// <summary>
/// Binds a variable name to an expression: either an <see cref="InputDeclaration"/>
/// or a <see cref="LocalDeclaration"/>.
/// </summary>
public abstract class Declaration
{
    private protected Declaration(string name, Expression value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        Name = name;
        Value = value;
    }

    /// <summary>The name of the declared variable, without the leading <c>$</c>.</summary>
    public string Name { get; }

    /// <summary>The expression whose resolved value the variable takes.</summary>
    public Expression Value { get; }
}
