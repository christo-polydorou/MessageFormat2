using System.Collections.Frozen;
using System.Globalization;
using System.Text;

using MessageFormat2.DataModel;
using MessageFormat2.Formatting;
using MessageFormat2.Functions;
using MessageFormat2.Syntax;
using MessageFormat2.Validation;

namespace MessageFormat2;

/// <summary>
/// Formats a MessageFormat 2 message for one culture. The message is parsed once, in the
/// constructor, and the formatter is immutable and thread-safe.
/// </summary>
/// <example>
/// <code>
/// var formatter = new MessageFormatter("Hello, {$name}!", CultureInfo.GetCultureInfo("en-US"));
/// string text = formatter.Format(new Dictionary&lt;string, object?&gt; { ["name"] = "World" });
/// </code>
/// </example>
public sealed class MessageFormatter
{
    /// <summary>Parses and validates a message.</summary>
    /// <param name="source">The message, in MessageFormat 2 syntax.</param>
    /// <param name="culture">The culture to format for.</param>
    /// <param name="options">Custom functions and other options, or null for the defaults.</param>
    /// <exception cref="MessageFormatException">The message has a syntax error or a data model error.</exception>
    public MessageFormatter(string source, CultureInfo culture, MessageFormatterOptions? options = null)
        : this(MessageParser.Parse(source), culture, options)
    {
    }

    /// <summary>Validates an already parsed or constructed message.</summary>
    /// <param name="message">The message.</param>
    /// <param name="culture">The culture to format for.</param>
    /// <param name="options">Custom functions and other options, or null for the defaults.</param>
    /// <exception cref="MessageFormatException">The message has a data model error.</exception>
    public MessageFormatter(Message message, CultureInfo culture, MessageFormatterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(culture);
        MessageValidator.Validate(message);

        options ??= new MessageFormatterOptions();

        Message = message;
        Culture = culture;
        BidiIsolation = options.BidiIsolation;
        MessageDirection = culture.TextInfo.IsRightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;
        Functions = BuildFunctionTable(options.Functions);
        DeclarationIndex = message.Declarations
            .Select((declaration, index) => (declaration.Name, index))
            .ToFrozenDictionary(entry => entry.Name, entry => entry.index, StringComparer.Ordinal);
    }

    /// <summary>The message being formatted.</summary>
    public Message Message { get; }

    /// <summary>The culture the message is formatted for.</summary>
    public CultureInfo Culture { get; }

    /// <summary>How placeholders are isolated in mixed-direction text.</summary>
    public BidiIsolation BidiIsolation { get; }

    internal TextDirection MessageDirection { get; }

    internal FrozenDictionary<string, MessageFunction> Functions { get; }

    internal FrozenDictionary<string, int> DeclarationIndex { get; }

    /// <summary>Formats the message to a string.</summary>
    /// <param name="arguments">Values for the message's variables, keyed by name without the <c>$</c>.</param>
    /// <returns>The formatted message. Placeholders that could not be formatted appear as fallbacks such as <c>{$name}</c>.</returns>
    public string Format(IReadOnlyDictionary<string, object?>? arguments = null) =>
        Format(arguments, out _);

    /// <summary>Formats the message to a string and reports any errors.</summary>
    /// <param name="arguments">Values for the message's variables, keyed by name without the <c>$</c>.</param>
    /// <param name="errors">The errors found while formatting, in the order they occurred. Empty if there were none.</param>
    /// <returns>The formatted message. Placeholders that could not be formatted appear as fallbacks such as <c>{$name}</c>.</returns>
    public string Format(IReadOnlyDictionary<string, object?>? arguments, out IReadOnlyList<MessageError> errors)
    {
        IReadOnlyList<MessagePart> parts = FormatToParts(arguments, out errors);

        var builder = new StringBuilder();
        foreach (MessagePart part in parts)
        {
            part.AppendTo(builder);
        }

        return builder.ToString();
    }

    /// <summary>Formats the message to a list of parts, so that markup and values can be rendered separately.</summary>
    /// <param name="arguments">Values for the message's variables, keyed by name without the <c>$</c>.</param>
    public IReadOnlyList<MessagePart> FormatToParts(IReadOnlyDictionary<string, object?>? arguments = null) =>
        FormatToParts(arguments, out _);

    /// <summary>Formats the message to a list of parts and reports any errors.</summary>
    /// <param name="arguments">Values for the message's variables, keyed by name without the <c>$</c>.</param>
    /// <param name="errors">The errors found while formatting, in the order they occurred. Empty if there were none.</param>
    public IReadOnlyList<MessagePart> FormatToParts(IReadOnlyDictionary<string, object?>? arguments, out IReadOnlyList<MessageError> errors)
    {
        var run = new FormattingRun(this, arguments);
        List<MessagePart> parts = run.FormatToParts();
        errors = run.Errors;
        return parts;
    }

    private static FrozenDictionary<string, MessageFunction> BuildFunctionTable(IDictionary<string, MessageFunction> custom)
    {
        var functions = new Dictionary<string, MessageFunction>(BuiltInFunctions.All, StringComparer.Ordinal);

        foreach ((string name, MessageFunction function) in custom)
        {
            ArgumentNullException.ThrowIfNull(function);

            // Function names in messages are normalized when parsed, so normalize these to match.
            string key = name.Normalize(NormalizationForm.FormC);
            if (key.StartsWith("u:", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Function name '{name}' is invalid: the 'u' namespace is reserved.", nameof(custom));
            }

            functions[key] = function;
        }

        return functions.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
