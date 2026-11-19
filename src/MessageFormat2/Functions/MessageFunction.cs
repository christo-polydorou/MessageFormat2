namespace MessageFormat2.Functions;

/// <summary>
/// A function that can be called from a message, such as <c>:number</c> or a custom <c>:my:money</c>.
/// The same instance may be called from several threads at once, so it must be thread-safe.
/// </summary>
public abstract class MessageFunction
{
    /// <summary>Creates a function.</summary>
    protected MessageFunction()
    {
    }

    /// <summary>Resolves one expression that uses this function.</summary>
    /// <param name="context">The locale and other context for the call.</param>
    /// <param name="operand">The resolved operand, or null if the expression has none (as in <c>{:fn}</c>).</param>
    /// <param name="options">The resolved options. Options whose value could not be resolved are left out.</param>
    /// <returns>The resolved value of the expression.</returns>
    /// <exception cref="MessageFunctionException">
    /// Throw this to fail. The placeholder is then shown as a fallback such as <c>{$amount}</c>
    /// and the error is reported to the caller.
    /// </exception>
    public abstract MessageValue Resolve(MessageFunctionContext context, MessageValue? operand, MessageOptionDictionary options);
}
