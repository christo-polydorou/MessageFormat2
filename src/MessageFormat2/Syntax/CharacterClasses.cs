namespace MessageFormat2.Syntax;

// Character classes from spec/message.abnf. Each method is named after the rule it implements.
internal static class CharacterClasses
{
    // ws = SP / HTAB / CR / LF / %x3000
    public static bool IsWhitespace(int c) =>
        c is ' ' or '\t' or '\r' or '\n' or '　';

    // bidi = %x061C / %x200E / %x200F / %x2066-2069
    public static bool IsBidi(int c) =>
        c is '؜' or '‎' or '‏' or (>= '⁦' and <= '⁩');

    // name-start: letters, +, _, and most non-ASCII code points except whitespace, bidi controls,
    // surrogates and noncharacters.
    public static bool IsNameStart(int c) =>
        c is (>= 'A' and <= 'Z')
            or (>= 'a' and <= 'z')
            or '+'
            or '_'
            or (>= 0xA1 and <= 0x61B)
            or (>= 0x61D and <= 0x167F)
            or (>= 0x1681 and <= 0x1FFF)
            or (>= 0x200B and <= 0x200D)
            or (>= 0x2010 and <= 0x2027)
            or (>= 0x2030 and <= 0x205E)
            or (>= 0x2060 and <= 0x2065)
            or (>= 0x206A and <= 0x2FFF)
            or (>= 0x3001 and <= 0xD7FF)
            or (>= 0xE000 and <= 0xFDCF)
            or (>= 0xFDF0 and <= 0xFFFD)
        // Supplementary planes, excluding the two noncharacters at the end of each plane.
        || (c is >= 0x10000 and <= 0x10FFFD && (c & 0xFFFF) <= 0xFFFD);

    // name-char = name-start / DIGIT / "-" / "."
    public static bool IsNameChar(int c) =>
        IsNameStart(c) || c is (>= '0' and <= '9') or '-' or '.';

    // text-char: any character except NUL, \, {, and }.
    public static bool IsTextChar(char c) =>
        c is not ('\0' or '\\' or '{' or '}');

    // quoted-char: any character except NUL, \, and |.
    public static bool IsQuotedChar(char c) =>
        c is not ('\0' or '\\' or '|');

    // what may follow a backslash in escaped-char
    public static bool IsEscapable(char c) =>
        c is '\\' or '{' or '|' or '}';
}
