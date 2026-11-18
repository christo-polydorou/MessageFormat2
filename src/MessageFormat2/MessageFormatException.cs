namespace MessageFormat2;

/// <summary>
/// Thrown when a message cannot be used: it has a syntax error or a data model error.
/// </summary>
/// <remarks>
/// Errors that happen while formatting a valid message do not throw by default; they are
/// reported alongside a best-effort result instead.
/// </remarks>
public sealed class MessageFormatException : Exception
{
    /// <summary>Creates an exception for the given error.</summary>
    /// <param name="errorType">The kind of error.</param>
    /// <param name="message">A description of the error.</param>
    /// <param name="position">The zero-based offset in the source where the error was found, if known.</param>
    public MessageFormatException(MessageErrorType errorType, string message, int? position = null)
        : base(position is null ? message : $"{message} (at position {position})")
    {
        ErrorType = errorType;
        Position = position;
    }

    /// <summary>The kind of error.</summary>
    public MessageErrorType ErrorType { get; }

    /// <summary>The zero-based offset in the source where the error was found, if known.</summary>
    public int? Position { get; }
}
