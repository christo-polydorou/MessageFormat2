namespace MessageFormat2.DataModel;

/// <summary>
/// One element of a <see cref="Pattern"/>: <see cref="Text"/>, an <see cref="Expression"/>,
/// or <see cref="Markup"/>. The spec may add new kinds, so handle unknown subclasses.
/// </summary>
public abstract class PatternElement
{
    private protected PatternElement()
    {
    }
}
