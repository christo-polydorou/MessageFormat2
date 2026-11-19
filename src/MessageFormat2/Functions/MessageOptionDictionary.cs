using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MessageFormat2.Functions;

/// <summary>
/// The resolved options of an expression, keyed by option name. Also records which options were
/// written as literals, since some (such as <c>select</c> on <c>:number</c>) must be.
/// </summary>
public sealed class MessageOptionDictionary : IReadOnlyDictionary<string, MessageValue>
{
    private readonly Dictionary<string, MessageValue> _values;
    private readonly HashSet<string> _literals;

    internal MessageOptionDictionary(Dictionary<string, MessageValue> values, HashSet<string> literals)
    {
        _values = values;
        _literals = literals;
    }

    /// <summary>An empty set of options.</summary>
    public static MessageOptionDictionary Empty { get; } = new([], []);

    /// <summary>Whether the option was written as a literal in the message.</summary>
    public bool IsLiteral(string name) => _literals.Contains(name);

    /// <inheritdoc/>
    public MessageValue this[string key] => _values[key];

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _values.Keys;

    /// <inheritdoc/>
    public IEnumerable<MessageValue> Values => _values.Values;

    /// <inheritdoc/>
    public int Count => _values.Count;

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out MessageValue value) => _values.TryGetValue(key, out value);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, MessageValue>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
