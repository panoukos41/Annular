using System.Text;

namespace Annular.Translate;

public sealed class TranslateDefaultParser : TranslateParser
{
    public static TranslateDefaultParser Instance { get; } = new();

    public override string Interpolate(string expr, TranslateParameters? parameters)
    {
        if (parameters?.Count is null or 0) return expr;

        var sb = new StringBuilder(expr);

        foreach (var param in parameters)
        {
            sb.Replace($"{{{param.Key}}}", param.Value);
        }
        return sb.ToString();
    }
}
