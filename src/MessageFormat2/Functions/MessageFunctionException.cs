namespace MessageFormat2.Functions;

/// <summary>
/// Thrown by a <see cref="MessageFunction"/> or <see cref="MessageValue"/> to report that it cannot
/// produce a result. The formatter catches it, records the error, and shows a fallback.
/// </summary>
public sealed class MessageFunctionException : Exception
{
    /// <summary>Creates an exception.</summary>
    /// <param name="errorType">The kind of error, usually <see cref="MessageErrorType.BadOperand"/> or <see cref="MessageErrorType.BadOption"/>.</param>
    /// <param name="message">A description of the error.</param>
    public MessageFunctionException(MessageErrorType errorType, string message)
        : base(message)
    {
        ErrorType = errorType;
    }

    /// <summary>The kind of error.</summary>
    public MessageErrorType ErrorType { get; }
}
