using System.Text;

using MessageFormat2.DataModel;

using static MessageFormat2.Syntax.CharacterClasses;

namespace MessageFormat2.Syntax;

// Recursive descent. Each Parse* method handles the rule from spec/message.abnf given in its
// comment: it starts at the current position, consumes the rule and leaves the position just after
// it. Any mismatch throws a syntax error.
internal ref struct Parser
{
    private const int EndOfInput = -1;

    private readonly ReadOnlySpan<char> _source;
    private readonly StringBuilder _textBuffer = new();
    private readonly StringBuilder _literalBuffer = new();
    private int _position;

    public Parser(ReadOnlySpan<char> source)
    {
        _source = source;
    }

    // message = simple-message / complex-message
    public Message ParseMessage()
    {
        // A complex message starts, after optional whitespace, with a keyword or "{{".
        // Anything else is a simple message, whose leading whitespace is part of its text.
        SkipOptionalWhitespace();
        bool isComplex = Peek() == '.' || LookingAt("{{");
        _position = 0;

        Message message = isComplex ? ParseComplexMessage() : ParseSimpleMessage();
        if (!AtEnd)
        {
            throw Error("Unexpected content after the end of the message");
        }

        return message;
    }

    // simple-message = o [simple-start pattern]
    private PatternMessage ParseSimpleMessage()
    {
        // The pattern rule covers the leading whitespace too, and keeps it as text.
        // ParseMessage has already checked that the first non-space character can start a simple message.
        Pattern pattern = ParsePattern();
        return new PatternMessage([], pattern);
    }

    // complex-message = o *(declaration o) complex-body o
    private Message ParseComplexMessage()
    {
        var declarations = new List<Declaration>();
        SkipOptionalWhitespace();

        while (true)
        {
            if (LookingAt(".input"))
            {
                declarations.Add(ParseInputDeclaration());
            }
            else if (LookingAt(".local"))
            {
                declarations.Add(ParseLocalDeclaration());
            }
            else if (LookingAt(".match"))
            {
                SelectMessage select = ParseMatcher(declarations);
                SkipOptionalWhitespace();
                return select;
            }
            else if (LookingAt("{{"))
            {
                Pattern pattern = ParseQuotedPattern();
                SkipOptionalWhitespace();
                return new PatternMessage(declarations, pattern);
            }
            else
            {
                throw Error("Expected a declaration, .match, or a quoted pattern");
            }

            SkipOptionalWhitespace();
        }
    }

    // input-declaration = input o variable-expression
    private InputDeclaration ParseInputDeclaration()
    {
        Expect(".input");
        SkipOptionalWhitespace();

        if (!LookingAtVariableExpression())
        {
            throw Error("Expected a variable expression after .input");
        }

        Expression expression = ParseExpression();
        var variable = (VariableRef)expression.Operand!;
        return new InputDeclaration(variable.Name, expression);
    }

    // local-declaration = local s variable o "=" o expression
    private LocalDeclaration ParseLocalDeclaration()
    {
        Expect(".local");
        RequireWhitespace();
        VariableRef variable = ParseVariable();
        SkipOptionalWhitespace();
        Expect('=');
        SkipOptionalWhitespace();

        if (Peek() != '{' || LookingAtMarkup())
        {
            throw Error("Expected an expression");
        }

        return new LocalDeclaration(variable.Name, ParseExpression());
    }

    // matcher = match-statement s variant *(o variant)
    // match-statement = match 1*(s selector)
    private SelectMessage ParseMatcher(List<Declaration> declarations)
    {
        Expect(".match");

        // Each selector is preceded by required whitespace. The whitespace after the
        // last selector is the "s" that separates the match statement from the first variant.
        var selectors = new List<VariableRef>();
        do
        {
            RequireWhitespace();
            selectors.Add(ParseVariable());
        }
        while (StartsWithWhitespaceThen('$'));

        RequireWhitespace();

        var variants = new List<Variant>();
        do
        {
            variants.Add(ParseVariant());
            SkipOptionalWhitespace();
        }
        while (LookingAtKey());

        return new SelectMessage(declarations, selectors, variants);
    }

    // variant = key *(s key) o quoted-pattern
    private Variant ParseVariant()
    {
        var keys = new List<VariantKey> { ParseKey() };

        while (true)
        {
            bool sawWhitespace = SkipOptionalWhitespace();
            if (LookingAt("{{"))
            {
                break;
            }

            if (!sawWhitespace)
            {
                throw Error("Expected whitespace between variant keys");
            }

            keys.Add(ParseKey());
        }

        return new Variant(keys, ParseQuotedPattern());
    }

    // key = literal / "*"
    private VariantKey ParseKey()
    {
        if (Peek() == '*')
        {
            _position++;
            return CatchallKey.Instance;
        }

        if (!LookingAtKey())
        {
            throw Error("Expected a variant key");
        }

        return new LiteralKey(ParseLiteral().Value);
    }

    // quoted-pattern = "{{" pattern "}}"
    private Pattern ParseQuotedPattern()
    {
        Expect("{{");
        Pattern pattern = ParsePattern();
        Expect("}}");
        return pattern;
    }

    // pattern = *(text-char / escaped-char / placeholder)
    // placeholder = expression / markup  Stops at the end of input or at a }, which the caller handles.
    private Pattern ParsePattern()
    {
        var elements = new List<PatternElement>();
        _textBuffer.Clear();

        while (!AtEnd && Peek() != '}')
        {
            char c = _source[_position];
            if (c == '{')
            {
                FlushText(elements);
                elements.Add(LookingAtMarkup() ? ParseMarkup() : ParseExpression());
            }
            else if (c == '\\')
            {
                _textBuffer.Append(ParseEscapedChar());
            }
            else if (IsTextChar(c))
            {
                _textBuffer.Append(c);
                _position++;
            }
            else
            {
                throw Error("Invalid character in pattern");
            }
        }

        FlushText(elements);
        return elements.Count == 0 ? Pattern.Empty : new Pattern(elements);
    }

    private void FlushText(List<PatternElement> elements)
    {
        if (_textBuffer.Length > 0)
        {
            elements.Add(new Text(_textBuffer.ToString()));
            _textBuffer.Clear();
        }
    }

    // expression = literal-expression / variable-expression / function-expression
    // literal-expression = "{" o literal [s function] *(s attribute) o "}"
    // variable-expression = "{" o variable [s function] *(s attribute) o "}"
    // function-expression = "{" o function *(s attribute) o "}"
    private Expression ParseExpression()
    {
        Expect('{');
        SkipOptionalWhitespace();

        Operand? operand = null;
        FunctionRef? function = null;

        if (Peek() == ':')
        {
            function = ParseFunction();
        }
        else
        {
            operand = Peek() == '$' ? ParseVariable()
                : LookingAtLiteral() ? ParseLiteral()
                : throw Error("Expected a literal, variable, or function in the expression");

            if (StartsWithWhitespaceThen(':'))
            {
                SkipOptionalWhitespace();
                function = ParseFunction();
            }
        }

        List<MessageAttribute> attributes = ParseAttributes();
        SkipOptionalWhitespace();
        Expect('}');

        return new Expression(operand, function, attributes);
    }

    // markup = "{" o "#" identifier *(s option) *(s attribute) o ["/"] "}"
    // / "{" o "/" identifier *(s option) *(s attribute) o "}"
    private Markup ParseMarkup()
    {
        Expect('{');
        SkipOptionalWhitespace();

        bool isClose = Peek() == '/';
        _position++; // the "#" or "/" sigil

        string name = ParseIdentifier();
        List<MessageOption> options = ParseOptions();
        List<MessageAttribute> attributes = ParseAttributes();
        SkipOptionalWhitespace();

        MarkupKind kind = MarkupKind.Close;
        if (!isClose)
        {
            kind = MarkupKind.Open;
            if (Peek() == '/')
            {
                _position++;
                kind = MarkupKind.Standalone;
            }
        }

        Expect('}');
        return new Markup(kind, name, options, attributes);
    }

    // function = ":" identifier *(s option)
    private FunctionRef ParseFunction()
    {
        Expect(':');
        string name = ParseIdentifier();
        return new FunctionRef(name, ParseOptions());
    }

    // *(s option), where option = identifier o "=" o (literal / variable)
    private List<MessageOption> ParseOptions()
    {
        var options = new List<MessageOption>();

        while (StartsWithWhitespaceThenNameStart())
        {
            SkipOptionalWhitespace();
            string name = ParseIdentifier();
            SkipOptionalWhitespace();
            Expect('=');
            SkipOptionalWhitespace();

            Operand value = Peek() == '$' ? ParseVariable() : ParseLiteral();
            options.Add(new MessageOption(name, value));
        }

        return options;
    }

    // *(s attribute), where attribute = "@" identifier [o "=" o literal]
    private List<MessageAttribute> ParseAttributes()
    {
        var attributes = new List<MessageAttribute>();

        while (StartsWithWhitespaceThen('@'))
        {
            SkipOptionalWhitespace();
            Expect('@');
            string name = ParseIdentifier();

            Literal? value = null;
            int beforeValue = _position;
            SkipOptionalWhitespace();
            if (Peek() == '=')
            {
                _position++;
                SkipOptionalWhitespace();
                value = ParseLiteral();
            }
            else
            {
                _position = beforeValue;
            }

            attributes.Add(new MessageAttribute(name, value));
        }

        return attributes;
    }

    // literal = quoted-literal / unquoted-literal
    // quoted-literal = "|" *(quoted-char / escaped-char) "|"
    // unquoted-literal = 1*name-char
    private Literal ParseLiteral()
    {
        if (Peek() != '|')
        {
            int start = _position;
            while (IsNameChar(PeekCodePoint(out int length)))
            {
                _position += length;
            }

            if (_position == start)
            {
                throw Error("Expected a literal");
            }

            return new Literal(_source[start.._position].ToString());
        }

        _position++; // opening "|"
        _literalBuffer.Clear();

        while (true)
        {
            if (AtEnd)
            {
                throw Error("Unterminated quoted literal");
            }

            char c = _source[_position];
            if (c == '|')
            {
                _position++;
                break;
            }

            if (c == '\\')
            {
                _literalBuffer.Append(ParseEscapedChar());
            }
            else if (IsQuotedChar(c))
            {
                _literalBuffer.Append(c);
                _position++;
            }
            else
            {
                throw Error("Invalid character in quoted literal");
            }
        }

        string value = _literalBuffer.ToString();
        _literalBuffer.Clear();
        return new Literal(value);
    }

    // variable = "$" name
    private VariableRef ParseVariable()
    {
        Expect('$');
        return new VariableRef(ParseName());
    }

    // identifier = [namespace ":"] name, where namespace = name
    private string ParseIdentifier()
    {
        string name = ParseName();
        if (Peek() != ':')
        {
            return name;
        }

        _position++;
        return $"{name}:{ParseName()}";
    }

    // name = [bidi] name-start *name-char [bidi]
    // Returns the name in NFC, without the optional bidi marks.
    private string ParseName()
    {
        if (IsBidi(Peek()))
        {
            _position++;
        }

        int start = _position;
        if (!IsNameStart(PeekCodePoint(out int length)))
        {
            throw Error("Expected a name");
        }

        _position += length;
        while (IsNameChar(PeekCodePoint(out length)))
        {
            _position += length;
        }

        string name = _source[start.._position].ToString();

        if (IsBidi(Peek()))
        {
            _position++;
        }

        // Names are compared as if normalized to NFC (spec: "Names and Identifiers").
        return name.Normalize(NormalizationForm.FormC);
    }

    // escaped-char = backslash ( backslash / "{" / "|" / "}" )
    private char ParseEscapedChar()
    {
        Expect('\\');
        if (AtEnd || !IsEscapable(_source[_position]))
        {
            throw Error("Invalid escape sequence");
        }

        return _source[_position++];
    }

    // o = *(ws / bidi)
    // Returns true if at least one whitespace character (not just bidi marks) was skipped.
    private bool SkipOptionalWhitespace()
    {
        bool sawWhitespace = false;
        while (!AtEnd)
        {
            char c = _source[_position];
            if (IsWhitespace(c))
            {
                sawWhitespace = true;
            }
            else if (!IsBidi(c))
            {
                break;
            }

            _position++;
        }

        return sawWhitespace;
    }

    // s = *bidi ws o
    private void RequireWhitespace()
    {
        if (!SkipOptionalWhitespace())
        {
            throw Error("Expected whitespace");
        }
    }

    private readonly bool AtEnd => _position >= _source.Length;

    private readonly int Peek() => AtEnd ? EndOfInput : _source[_position];

    // An unpaired surrogate is returned as-is; no name character accepts it.
    private readonly int PeekCodePoint(out int length)
    {
        length = 1;
        if (AtEnd)
        {
            return EndOfInput;
        }

        char c = _source[_position];
        if (char.IsHighSurrogate(c) && _position + 1 < _source.Length && char.IsLowSurrogate(_source[_position + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(c, _source[_position + 1]);
        }

        return c;
    }

    private readonly bool LookingAt(string text) => _source[_position..].StartsWith(text, StringComparison.Ordinal);

    private readonly bool LookingAtMarkup()
    {
        var lookahead = this;
        lookahead._position++; // "{"
        lookahead.SkipOptionalWhitespace();
        return lookahead.Peek() is '#' or '/';
    }

    private readonly bool LookingAtVariableExpression()
    {
        if (Peek() != '{')
        {
            return false;
        }

        var lookahead = this;
        lookahead._position++; // "{"
        lookahead.SkipOptionalWhitespace();
        return lookahead.Peek() == '$';
    }

    private readonly bool LookingAtKey() => Peek() == '*' || LookingAtLiteral();

    private readonly bool LookingAtLiteral() => Peek() == '|' || IsNameChar(PeekCodePoint(out _));

    private readonly bool StartsWithWhitespaceThen(char next)
    {
        var lookahead = this;
        return lookahead.SkipOptionalWhitespace() && lookahead.Peek() == next;
    }

    private readonly bool StartsWithWhitespaceThenNameStart()
    {
        var lookahead = this;
        if (!lookahead.SkipOptionalWhitespace())
        {
            return false;
        }

        if (IsBidi(lookahead.Peek()))
        {
            lookahead._position++;
        }

        return IsNameStart(lookahead.PeekCodePoint(out _));
    }

    private void Expect(char expected)
    {
        if (Peek() != expected)
        {
            throw Error($"Expected '{expected}'");
        }

        _position++;
    }

    private void Expect(string expected)
    {
        if (!LookingAt(expected))
        {
            throw Error($"Expected '{expected}'");
        }

        _position += expected.Length;
    }

    private readonly MessageFormatException Error(string description)
    {
        string found = AtEnd ? "end of input" : $"'{_source[_position]}'";
        return new MessageFormatException(MessageErrorType.SyntaxError, $"{description}, found {found}", _position);
    }
}
