using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>
/// A placeholder or declaration value in braces: an optional operand, an optional function,
/// and any attributes. At least one of <see cref="Operand"/> and <see cref="Function"/> is set.
/// </summary>
/// <example><c>{$count :number minimumFractionDigits=2}</c></example>
public sealed class Expression : PatternElement
{
    /// <summary>Creates an expression.</summary>
    /// <exception cref="ArgumentException">Both <paramref name="operand"/> and <paramref name="function"/> are null.</exception>
    public Expression(Operand? operand, FunctionRef? function, IEnumerable<MessageAttribute>? attributes = null)
    {
        if (operand is null && function is null)
        {
            throw new ArgumentException("An expression needs an operand, a function, or both.");
        }

        Operand = operand;
        Function = function;
        Attributes = ReadOnly.Copy(attributes ?? []);
    }

    /// <summary>The literal or variable the expression operates on, if any.</summary>
    public Operand? Operand { get; }

    /// <summary>The function applied to the operand, if any.</summary>
    public FunctionRef? Function { get; }

    /// <summary>The attributes, in source order. Attributes have no effect on formatting.</summary>
    public IReadOnlyList<MessageAttribute> Attributes { get; }
}
