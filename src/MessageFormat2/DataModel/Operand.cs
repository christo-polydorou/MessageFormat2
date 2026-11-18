namespace MessageFormat2.DataModel;

/// <summary>
/// A value written in a message: either a <see cref="Literal"/> or a <see cref="VariableRef"/>.
/// Used as the operand of an expression and as the value of an option.
/// </summary>
public abstract class Operand
{
    private protected Operand()
    {
    }
}
