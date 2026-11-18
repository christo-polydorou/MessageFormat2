namespace MessageFormat2.DataModel;

/// <summary>A named option of a function or markup, such as <c>minimumFractionDigits=2</c>.</summary>
public sealed class MessageOption
{
    /// <summary>Creates an option.</summary>
    public MessageOption(string name, Operand value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        Name = name;
        Value = value;
    }

    /// <summary>The option identifier. May include a namespace, as in <c>u:dir</c>.</summary>
    public string Name { get; }

    /// <summary>The option's value: a literal or a variable.</summary>
    public Operand Value { get; }
}
