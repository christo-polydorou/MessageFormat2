using System.Globalization;

namespace MessageFormat2.Functions;

/// <summary>Information passed to a <see cref="MessageFunction"/> about the expression being resolved.</summary>
public sealed class MessageFunctionContext
{
    private readonly Action<MessageError> _reportError;

    internal MessageFunctionContext(CultureInfo culture, TextDirection direction, Action<MessageError> reportError)
    {
        Culture = culture;
        Direction = direction;
        _reportError = reportError;
    }

    /// <summary>The culture to format for.</summary>
    public CultureInfo Culture { get; }

    /// <summary>The locale to format for, as a BCP 47 tag.</summary>
    public string Locale => Culture.Name;

    /// <summary>
    /// The base direction of the expression: <see cref="TextDirection.Unknown"/> unless <c>u:dir</c>
    /// is <c>ltr</c> or <c>rtl</c>. The formatter applies <c>u:dir</c> to the result itself.
    /// </summary>
    public TextDirection Direction { get; }

    /// <summary>Reports an error that does not stop the function from returning a value.</summary>
    /// <example>An option with a value the function does not recognize, which it then ignores.</example>
    public void ReportError(MessageErrorType type, string description) =>
        _reportError(new MessageError(type, description));
}
