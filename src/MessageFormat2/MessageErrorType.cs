namespace MessageFormat2;

/// <summary>The kinds of error defined by the MessageFormat 2 specification.</summary>
/// <remarks>See the "Errors" section of the specification for when each one occurs.</remarks>
public enum MessageErrorType
{
    // Syntax errors

    /// <summary>The message source is not well-formed.</summary>
    SyntaxError,

    // Data model errors

    /// <summary>A variant has a different number of keys than there are selectors.</summary>
    VariantKeyMismatch,

    /// <summary>No variant has only catch-all (<c>*</c>) keys.</summary>
    MissingFallbackVariant,

    /// <summary>A selector does not directly or indirectly reference a declaration with a function.</summary>
    MissingSelectorAnnotation,

    /// <summary>A variable is declared more than once, or declared after being used.</summary>
    DuplicateDeclaration,

    /// <summary>The same option name appears more than once in one function or markup.</summary>
    DuplicateOptionName,

    /// <summary>Two variants have the same list of keys.</summary>
    DuplicateVariant,

    // Resolution errors

    /// <summary>A variable reference cannot be resolved.</summary>
    UnresolvedVariable,

    /// <summary>A function reference cannot be resolved.</summary>
    UnknownFunction,

    /// <summary>A selector could not be resolved or could not select.</summary>
    BadSelector,

    // Message function errors

    /// <summary>A function was given an operand it cannot handle.</summary>
    BadOperand,

    /// <summary>A function was given an option value it cannot handle.</summary>
    BadOption,

    /// <summary>A variant key is not valid for the selector's function.</summary>
    BadVariantKey,

    /// <summary>A function does not support the requested operation, such as formatting or selection.</summary>
    UnsupportedOperation,

    /// <summary>Any other error raised by a function.</summary>
    MessageFunctionError,
}
