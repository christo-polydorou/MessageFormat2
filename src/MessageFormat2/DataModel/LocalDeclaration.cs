namespace MessageFormat2.DataModel;

/// <summary>A <c>.local</c> declaration, which creates a new variable from an expression.</summary>
/// <example><c>.local $total = {$price :number style=percent}</c></example>
public sealed class LocalDeclaration : Declaration
{
    /// <summary>Creates a local declaration.</summary>
    /// <param name="name">The variable name, without the leading <c>$</c>.</param>
    /// <param name="value">The expression to bind.</param>
    public LocalDeclaration(string name, Expression value)
        : base(name, value)
    {
    }
}
