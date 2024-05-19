namespace Annular.Translate;

public abstract class TranslateParser
{
    /// <summary>
    /// Interpolates a string to replace parameters.
    /// </summary>
    /// <remarks>eg: "This is a {key}" ==> "This is a value", with params = { key: "value" }</remarks>
    public abstract string Interpolate(string expr, TranslateParameters? parameters);
}
