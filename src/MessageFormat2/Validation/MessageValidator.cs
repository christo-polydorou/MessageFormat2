using System.Text;

using MessageFormat2.DataModel;

namespace MessageFormat2.Validation;

/// <summary>
/// Checks a parsed <see cref="Message"/> for Data Model Errors: the rules a message must
/// follow beyond being syntactically well-formed.
/// </summary>
/// <remarks>See the "Data Model Errors" section of the specification.</remarks>
public static class MessageValidator
{
    /// <summary>Checks a message and throws if it is not valid.</summary>
    /// <exception cref="MessageFormatException">The message has a data model error.</exception>
    public static void Validate(Message message)
    {
        IReadOnlyList<MessageError> errors = GetErrors(message);
        if (errors.Count > 0)
        {
            throw new MessageFormatException(errors[0].Type, errors[0].Description);
        }
    }

    /// <summary>Returns every data model error in a message, or an empty list if it is valid.</summary>
    public static IReadOnlyList<MessageError> GetErrors(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var errors = new List<MessageError>();
        CheckDeclarations(message.Declarations, errors);
        CheckOptionNames(message, errors);

        if (message is SelectMessage select)
        {
            CheckVariantKeyCounts(select, errors);
            CheckFallbackVariant(select, errors);
            CheckSelectorAnnotations(select, errors);
            CheckDuplicateVariants(select, errors);
        }

        return errors;
    }

    // A variable can't be declared twice, can't be declared after an earlier declaration used it,
    // and a declaration can't refer to its own variable.
    private static void CheckDeclarations(IReadOnlyList<Declaration> declarations, List<MessageError> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Declaration declaration in declarations)
        {
            IEnumerable<string> referenced = declaration is InputDeclaration
                ? VariablesIn(declaration.Value.Function) // the operand is the declared variable itself
                : VariablesIn(declaration.Value);

            if (seen.Contains(declaration.Name))
            {
                errors.Add(new(MessageErrorType.DuplicateDeclaration,
                    $"Variable ${declaration.Name} is declared after it was already declared or used."));
            }
            else if (referenced.Contains(declaration.Name, StringComparer.Ordinal))
            {
                errors.Add(new(MessageErrorType.DuplicateDeclaration,
                    $"The declaration of ${declaration.Name} refers to itself."));
            }

            seen.Add(declaration.Name);
            seen.UnionWith(VariablesIn(declaration.Value));
        }
    }

    private static void CheckOptionNames(Message message, List<MessageError> errors)
    {
        foreach (IReadOnlyList<MessageOption> options in AllOptionLists(message))
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (MessageOption option in options)
            {
                if (!names.Add(option.Name))
                {
                    errors.Add(new(MessageErrorType.DuplicateOptionName,
                        $"Option '{option.Name}' appears more than once."));
                }
            }
        }
    }

    private static void CheckVariantKeyCounts(SelectMessage message, List<MessageError> errors)
    {
        foreach (Variant variant in message.Variants)
        {
            if (variant.Keys.Count != message.Selectors.Count)
            {
                errors.Add(new(MessageErrorType.VariantKeyMismatch,
                    $"A variant has {variant.Keys.Count} keys but there are {message.Selectors.Count} selectors."));
            }
        }
    }

    private static void CheckFallbackVariant(SelectMessage message, List<MessageError> errors)
    {
        bool hasFallback = message.Variants.Any(variant => variant.Keys.All(key => key is CatchallKey));
        if (!hasFallback)
        {
            errors.Add(new(MessageErrorType.MissingFallbackVariant,
                "No variant has only catch-all (*) keys."));
        }
    }

    // Each selector must refer, directly or through other local variables, to a declaration with a
    // function.
    private static void CheckSelectorAnnotations(SelectMessage message, List<MessageError> errors)
    {
        foreach (VariableRef selector in message.Selectors)
        {
            if (!HasAnnotation(selector.Name, message.Declarations))
            {
                errors.Add(new(MessageErrorType.MissingSelectorAnnotation,
                    $"Selector ${selector.Name} does not refer to a declaration with a function."));
            }
        }
    }

    private static bool HasAnnotation(string variableName, IReadOnlyList<Declaration> declarations)
    {
        // Follow the chain of declarations back from the selector. Each step moves to an
        // earlier declaration (later ones cannot be referenced), so this always terminates.
        string? name = variableName;
        int searchBefore = declarations.Count;

        while (name is not null)
        {
            int index = LastIndexOfDeclaration(declarations, name, searchBefore);
            if (index < 0)
            {
                return false;
            }

            Expression value = declarations[index].Value;
            if (value.Function is not null)
            {
                return true;
            }

            name = declarations[index] is LocalDeclaration && value.Operand is VariableRef next ? next.Name : null;
            searchBefore = index;
        }

        return false;
    }

    private static int LastIndexOfDeclaration(IReadOnlyList<Declaration> declarations, string name, int before)
    {
        for (int i = before - 1; i >= 0; i--)
        {
            if (declarations[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    // Literal keys are compared after NFC normalization. The literal |*| is not the catch-all *.
    private static void CheckDuplicateVariants(SelectMessage message, List<MessageError> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Variant variant in message.Variants)
        {
            if (!seen.Add(KeySignature(variant.Keys)))
            {
                errors.Add(new(MessageErrorType.DuplicateVariant,
                    "Two variants have the same keys."));
            }
        }
    }

    // Equal for two key lists exactly when the keys are equal. Literal values can't contain U+0000,
    // so it is safe as a separator.
    private static string KeySignature(IReadOnlyList<VariantKey> keys)
    {
        var signature = new StringBuilder();
        foreach (VariantKey key in keys)
        {
            signature.Append(key switch
            {
                CatchallKey => "*",
                LiteralKey literal => "=" + literal.Value.Normalize(NormalizationForm.FormC),
                _ => throw new NotSupportedException($"Unknown key type {key.GetType()}."),
            });
            signature.Append('\0');
        }

        return signature.ToString();
    }

    private static IEnumerable<string> VariablesIn(Expression expression)
    {
        if (expression.Operand is VariableRef variable)
        {
            yield return variable.Name;
        }

        foreach (string name in VariablesIn(expression.Function))
        {
            yield return name;
        }
    }

    private static IEnumerable<string> VariablesIn(FunctionRef? function) =>
        function is null
            ? []
            : function.Options.Select(option => option.Value).OfType<VariableRef>().Select(variable => variable.Name);

    private static IEnumerable<IReadOnlyList<MessageOption>> AllOptionLists(Message message)
    {
        IEnumerable<PatternElement> elements = message switch
        {
            PatternMessage pattern => pattern.Pattern.Elements,
            SelectMessage select => select.Variants.SelectMany(variant => variant.Value.Elements),
            _ => [],
        };

        IEnumerable<PatternElement> declarationValues = message.Declarations.Select(declaration => declaration.Value);

        foreach (PatternElement element in declarationValues.Concat(elements))
        {
            switch (element)
            {
                case Expression { Function: { } function }:
                    yield return function.Options;
                    break;
                case Markup markup:
                    yield return markup.Options;
                    break;
            }
        }
    }
}
