using MessageFormat2.DataModel;

namespace MessageFormat2.Syntax;

/// <summary>Parses MessageFormat 2 source text into the <see cref="Message"/> data model.</summary>
public static class MessageParser
{
    /// <summary>Parses a message.</summary>
    /// <param name="source">The message, in MessageFormat 2 syntax.</param>
    /// <returns>The parsed message. It has not been checked for data model errors.</returns>
    /// <exception cref="MessageFormatException">The source has a syntax error.</exception>
    public static Message Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new Parser(source).ParseMessage();
    }
}
