namespace MessageFormat2.DataModel;

/// <summary>
/// An <c>.input</c> declaration, which annotates an external variable passed in by the caller.
/// </summary>
/// <example><c>.input {$count :number}</c></example>
public sealed class InputDeclaration : Declaration
{
    /// <summary>Creates an input declaration.</summary>
    /// <param name="name">The variable name, without the leading <c>$</c>.</param>
    /// <param name="value">An expression whose operand is a variable named <paramref name="name"/>.</param>
    public InputDeclaration(string name, Expression value)
        : base(name, value)
    {
        if (value.Operand is not VariableRef variable || variable.Name != name)
        {
            throw new ArgumentException(
                "The operand of an input declaration must be the declared variable.", nameof(value));
        }
    }
}
