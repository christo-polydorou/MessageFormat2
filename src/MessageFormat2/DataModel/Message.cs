using MessageFormat2.Internal;

namespace MessageFormat2.DataModel;

/// <summary>
/// A parsed message: either a <see cref="PatternMessage"/> or a <see cref="SelectMessage"/>.
/// Instances are immutable and safe to share between threads.
/// </summary>
public abstract class Message
{
    private protected Message(IEnumerable<Declaration> declarations)
    {
        Declarations = ReadOnly.Copy(declarations);
    }

    /// <summary>The <c>.input</c> and <c>.local</c> declarations, in source order.</summary>
    public IReadOnlyList<Declaration> Declarations { get; }
}
