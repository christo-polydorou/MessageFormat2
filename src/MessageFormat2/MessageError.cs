namespace MessageFormat2;

/// <summary>An error found while validating or formatting a message.</summary>
public sealed class MessageError
{
    /// <summary>Creates an error.</summary>
    /// <param name="type">The kind of error.</param>
    /// <param name="description">A human-readable description of the error.</param>
    /// <param name="exception">The exception that caused the error, if any.</param>
    public MessageError(MessageErrorType type, string description, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(description);
        Type = type;
        Description = description;
        Exception = exception;
    }

    /// <summary>The kind of error.</summary>
    public MessageErrorType Type { get; }

    /// <summary>A human-readable description of the error.</summary>
    public string Description { get; }

    /// <summary>
    /// The exception that caused the error, if any. Set when a function fails with an
    /// unexpected exception rather than a <see cref="Functions.MessageFunctionException"/>.
    /// </summary>
    public Exception? Exception { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Type}: {Description}";
}
