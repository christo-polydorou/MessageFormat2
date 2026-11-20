using System.Text;

using MessageFormat2.DataModel;
using MessageFormat2.Functions;
using MessageFormat2.Functions.Numbers;

namespace MessageFormat2.Formatting;

// State for one Format call: arguments, declarations resolved so far, and errors.
// Declarations are resolved lazily and at most once.
internal sealed class FormattingRun
{
    private readonly MessageFormatter _formatter;
    private readonly IReadOnlyDictionary<string, object?> _arguments;
    private readonly MessageValue?[] _declarationValues;
    private readonly List<MessageError> _errors = [];

    public FormattingRun(MessageFormatter formatter, IReadOnlyDictionary<string, object?>? arguments)
    {
        _formatter = formatter;
        _arguments = arguments ?? EmptyArguments;
        _declarationValues = new MessageValue?[formatter.Message.Declarations.Count];
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyArguments = new Dictionary<string, object?>();

    public IReadOnlyList<MessageError> Errors => _errors;

    public List<MessagePart> FormatToParts()
    {
        Pattern pattern = _formatter.Message switch
        {
            PatternMessage message => message.Pattern,
            SelectMessage message => SelectPattern(message),
            _ => throw new NotSupportedException($"Unknown message type {_formatter.Message.GetType()}."),
        };

        return FormatPattern(pattern);
    }

    private Pattern SelectPattern(SelectMessage message)
    {
        MessageValue?[] selectors = message.Selectors
            .Select(selector => ResolveSelector(selector))
            .ToArray();

        Variant? best = null;
        foreach (Variant variant in message.Variants)
        {
            if (!SelectorsMatch(selectors, variant.Keys))
            {
                continue;
            }

            if (best is null || SelectorsCompare(selectors, variant.Keys, best.Keys))
            {
                best = variant;
            }
        }

        // Validation guarantees a variant with only catch-all keys, which always matches.
        return best!.Value;
    }

    // Returns null (after reporting Bad Selector) if the value can't select.
    private MessageValue? ResolveSelector(VariableRef selector)
    {
        MessageValue value = ResolveVariable(selector.Name);
        if (value is FallbackValue || !value.SupportsSelection)
        {
            ReportError(MessageErrorType.BadSelector, $"${selector.Name} cannot be used as a selector.");
            return null;
        }

        return value;
    }

    private bool SelectorsMatch(MessageValue?[] selectors, IReadOnlyList<VariantKey> keys)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] is LiteralKey key && !Match(selectors, i, NormalizeKey(key)))
            {
                return false;
            }
        }

        return true;
    }

    private bool SelectorsCompare(MessageValue?[] selectors, IReadOnlyList<VariantKey> keys1, IReadOnlyList<VariantKey> keys2)
    {
        for (int i = 0; i < keys1.Count; i++)
        {
            switch (keys1[i], keys2[i])
            {
                case (CatchallKey, CatchallKey):
                    continue;
                case (CatchallKey, _):
                    return false;
                case (_, CatchallKey):
                    return true;
                case (LiteralKey literal1, LiteralKey literal2):
                    string key1 = NormalizeKey(literal1);
                    string key2 = NormalizeKey(literal2);
                    if (key1 == key2)
                    {
                        continue;
                    }

                    return BetterThan(selectors, i, key1, key2);
            }
        }

        return false;
    }

    private bool Match(MessageValue?[] selectors, int index, string key) =>
        CallSelector(selectors, index, value => value.Matches(key));

    private bool BetterThan(MessageValue?[] selectors, int index, string key1, string key2) =>
        CallSelector(selectors, index, value => value.IsBetterMatch(key1, key2));

    // If a selection method fails, the selector is disabled for the rest of selection (it then
    // matches only *) and Bad Selector is reported once.
    private bool CallSelector(MessageValue?[] selectors, int index, Func<MessageValue, bool> operation)
    {
        if (selectors[index] is not { } selector)
        {
            return false;
        }

        try
        {
            return operation(selector);
        }
        catch (MessageFunctionException exception) when (exception.ErrorType == MessageErrorType.BadVariantKey)
        {
            // A key that is invalid for this selector does not match; selection carries on.
            ReportError(exception.ErrorType, exception.Message);
            return false;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            selectors[index] = null;
            _errors.Add(new MessageError(MessageErrorType.BadSelector, "Selection failed.", exception));
            return false;
        }
    }

    private static string NormalizeKey(LiteralKey key) => key.Value.Normalize(NormalizationForm.FormC);

    private List<MessagePart> FormatPattern(Pattern pattern)
    {
        var parts = new List<MessagePart>(pattern.Elements.Count);

        foreach (PatternElement element in pattern.Elements)
        {
            switch (element)
            {
                case Text text:
                    parts.Add(new TextPart(text.Value));
                    break;
                case Markup markup:
                    parts.Add(ResolveMarkup(markup));
                    break;
                case Expression expression:
                    AddPlaceholder(parts, expression);
                    break;
                default:
                    throw new NotSupportedException($"Unknown pattern element {element.GetType()}.");
            }
        }

        return parts;
    }

    private void AddPlaceholder(List<MessagePart> parts, Expression expression)
    {
        MessagePart part = FormatExpression(expression);
        (TextDirection direction, bool isolate) = part is ExpressionPart formatted
            ? (formatted.Direction, formatted.Isolate)
            : (TextDirection.Unknown, false);

        if (_formatter.BidiIsolation == BidiIsolation.None)
        {
            parts.Add(part);
            return;
        }

        // The Default Bidi Strategy (spec: "Handling Bidirectional Text").
        BidiIsolationPart? opening = direction switch
        {
            TextDirection.LeftToRight when _formatter.MessageDirection == TextDirection.LeftToRight && !isolate => null,
            TextDirection.LeftToRight => BidiIsolationPart.LeftToRightIsolate,
            TextDirection.RightToLeft => BidiIsolationPart.RightToLeftIsolate,
            _ => BidiIsolationPart.FirstStrongIsolate,
        };

        if (opening is null)
        {
            parts.Add(part);
        }
        else
        {
            parts.Add(opening);
            parts.Add(part);
            parts.Add(BidiIsolationPart.PopDirectionalIsolate);
        }
    }

    private MessagePart FormatExpression(Expression expression)
    {
        MessageValue value = ResolveExpression(expression);
        if (value is FallbackValue)
        {
            return new FallbackPart(FallbackSource(expression));
        }

        try
        {
            var annotated = value as AnnotatedValue;
            return new ExpressionPart(
                value.Type,
                value.FormatToString(),
                value.FormatToParts(),
                value.Locale,
                value.Direction,
                annotated?.Id,
                annotated?.Isolate ?? false);
        }
        catch (MessageFunctionException exception)
        {
            ReportError(exception.ErrorType, exception.Message);
            return new FallbackPart(FallbackSource(expression));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _errors.Add(new MessageError(MessageErrorType.MessageFunctionError, exception.Message, exception));
            return new FallbackPart(FallbackSource(expression));
        }
    }

    private MarkupPart ResolveMarkup(Markup markup)
    {
        MessageOptionDictionary options = ResolveOptions(markup.Options, out UnicodeOptions unicodeOptions);
        if (unicodeOptions.Direction is not null)
        {
            ReportError(MessageErrorType.BadOption, $"The {UnicodeOptions.DirectionName} option cannot be used on markup.");
        }

        var values = options.ToDictionary(option => option.Key, option => option.Value.Value, StringComparer.Ordinal);
        return new MarkupPart(markup.Kind, markup.Name, values, unicodeOptions.Id);
    }

    private MessageValue ResolveExpression(Expression expression) =>
        ResolveExpression(expression, expression.Operand is null ? null : ResolveOperand(expression.Operand));

    private MessageValue ResolveExpression(Expression expression, MessageValue? operand)
    {
        if (expression.Function is not { } function)
        {
            return operand!;
        }

        if (!_formatter.Functions.TryGetValue(function.Name, out MessageFunction? handler))
        {
            ReportError(MessageErrorType.UnknownFunction, $"Unknown function :{function.Name}.");
            return FallbackValue.Instance;
        }

        if (operand is FallbackValue)
        {
            ReportError(MessageErrorType.BadOperand, $"The operand of :{function.Name} could not be resolved.");
            return FallbackValue.Instance;
        }

        // The u: options are not passed to the function; they are attached to its result instead.
        MessageOptionDictionary options = ResolveOptions(function.Options, out UnicodeOptions unicodeOptions);
        var context = new MessageFunctionContext(_formatter.Culture, unicodeOptions.BaseDirection, _errors.Add);

        try
        {
            return unicodeOptions.ApplyTo(handler.Resolve(context, AnnotatedValue.Unwrap(operand), options));
        }
        catch (MessageFunctionException exception)
        {
            ReportError(exception.ErrorType, exception.Message);
            return FallbackValue.Instance;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _errors.Add(new MessageError(
                MessageErrorType.MessageFunctionError, $":{function.Name} failed: {exception.Message}", exception));
            return FallbackValue.Instance;
        }
    }

    // Options that fail to resolve are left out, with a Bad Option error. u:id and u:dir are pulled
    // out and returned separately.
    private MessageOptionDictionary ResolveOptions(IReadOnlyList<MessageOption> options, out UnicodeOptions unicodeOptions)
    {
        unicodeOptions = default;
        if (options.Count == 0)
        {
            return MessageOptionDictionary.Empty;
        }

        var values = new Dictionary<string, MessageValue>(options.Count, StringComparer.Ordinal);
        var literals = new HashSet<string>(StringComparer.Ordinal);

        foreach (MessageOption option in options)
        {
            MessageValue value = ResolveOperand(option.Value);
            if (value is FallbackValue)
            {
                ReportError(MessageErrorType.BadOption, $"The value of option '{option.Name}' could not be resolved.");
                continue;
            }

            switch (option.Name)
            {
                case UnicodeOptions.IdName:
                    unicodeOptions = unicodeOptions with { Id = ResolveIdOption(value) };
                    continue;
                case UnicodeOptions.DirectionName:
                    unicodeOptions = unicodeOptions with { Direction = ResolveDirectionOption(value) };
                    continue;
            }

            values[option.Name] = AnnotatedValue.Unwrap(value);
            if (option.Value is Literal)
            {
                literals.Add(option.Name);
            }
        }

        return new MessageOptionDictionary(values, literals);
    }

    private string? ResolveIdOption(MessageValue value)
    {
        try
        {
            return value is StringValue text ? text.Text : value.FormatToString();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _errors.Add(new MessageError(
                MessageErrorType.BadOption, $"The value of {UnicodeOptions.IdName} cannot be converted to a string.", exception));
            return null;
        }
    }

    private DirectionOption? ResolveDirectionOption(MessageValue value)
    {
        DirectionOption? direction = value.Value is string text ? UnicodeOptions.ParseDirection(text) : null;
        if (direction is null)
        {
            ReportError(MessageErrorType.BadOption, $"The value of {UnicodeOptions.DirectionName} must be ltr, rtl, auto, or inherit.");
        }

        return direction;
    }

    private MessageValue ResolveOperand(Operand operand) => operand switch
    {
        Literal literal => new StringValue(literal.Value),
        VariableRef variable => ResolveVariable(variable.Name),
        _ => throw new NotSupportedException($"Unknown operand type {operand.GetType()}."),
    };

    private MessageValue ResolveVariable(string name)
    {
        if (!_formatter.DeclarationIndex.TryGetValue(name, out int index))
        {
            return ResolveArgument(name);
        }

        MessageValue? value = _declarationValues[index];
        if (value is null)
        {
            Declaration declaration = _formatter.Message.Declarations[index];

            // The operand of an .input declaration is the caller's argument of the same name.
            value = declaration is InputDeclaration
                ? ResolveExpression(declaration.Value, ResolveArgument(name))
                : ResolveExpression(declaration.Value);
            _declarationValues[index] = value;
        }

        return value;
    }

    private MessageValue ResolveArgument(string name)
    {
        if (!TryGetArgument(name, out object? argument))
        {
            ReportError(MessageErrorType.UnresolvedVariable, $"No value was provided for ${name}.");
            return FallbackValue.Instance;
        }

        return argument switch
        {
            MessageValue value => value,
            string text => new StringValue(text),

            // Numbers used without a function behave as if annotated with :number.
            _ when IsNumeric(argument) && DecimalNumber.TryFrom(argument, out DecimalNumber number)
                => new NumberValue(number, NumberOptions.Default, _formatter.Culture),

            CurrencyAmount { Currency: not null } amount when DecimalNumber.TryFrom(amount.Amount, out DecimalNumber number)
                => new NumberValue(number, NumberOptions.Default with { Style = NumberStyle.Currency, Currency = amount.Currency }, _formatter.Culture),
            DateTime or DateTimeOffset or DateOnly or TimeOnly => DateTimeValue.FromArgument(argument, _formatter.Culture)!,
            _ => new InputValue(argument, _formatter.Culture),
        };
    }

    private static bool IsNumeric(object? value) =>
        value is int or long or short or sbyte or byte or ushort or uint or ulong
            or float or double or decimal or Half or System.Numerics.BigInteger or Int128 or UInt128;

    // Names are compared as if both were in NFC.
    private bool TryGetArgument(string name, out object? value)
    {
        // Names in the message are already normalized. Most argument names are too, so
        // only scan the argument names when the direct lookup misses.
        if (_arguments.TryGetValue(name, out value))
        {
            return true;
        }

        foreach ((string key, object? candidate) in _arguments)
        {
            if (!key.IsNormalized(NormalizationForm.FormC) && key.Normalize(NormalizationForm.FormC) == name)
            {
                value = candidate;
                return true;
            }
        }

        value = null;
        return false;
    }

    private void ReportError(MessageErrorType type, string description) =>
        _errors.Add(new MessageError(type, description));

    // spec: "Fallback Resolution"
    private static string FallbackSource(Expression expression) => expression switch
    {
        { Operand: Literal literal } => "|" + literal.Value.Replace(@"\", @"\\", StringComparison.Ordinal).Replace("|", @"\|", StringComparison.Ordinal) + "|",
        { Operand: VariableRef variable } => "$" + variable.Name,
        { Function: { } function } => ":" + function.Name,
        _ => "�",
    };
}
