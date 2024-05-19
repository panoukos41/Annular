using System.Text.RegularExpressions;

namespace Annular.Translate;

public readonly ref partial struct TranslateString
{
    private readonly string value;
    private readonly TranslateParameters? parameters;
    private readonly TranslateParser parser;

    public TranslateString(string value, TranslateParameters? parameters = null, TranslateParser? parser = null)
    {
        this.value = value;
        this.parameters = parameters;
        this.parser = parser ?? TranslateDefaultParser.Instance;
    }

    public override string ToString()
    {
        return parser.Interpolate(value, parameters);
    }

    public TranslateString Add(string key, string value)
    {
        if (parameters is { })
        {
            parameters.Set(key, value);
            return new(this.value, parameters, parser);
        }

#if NET8_0_OR_GREATER
        if (!ParameterRegex().IsMatch(this.value)) return this;
#else
        if (!Regex.IsMatch(this.value, @"{.+?(?=})")) return this;
#endif
        return new(this.value, new(key, value), parser);
    }

    public static TranslateString operator |(TranslateString str, (string key, string value) param)
    {
        return str.Add(param.key, param.value);
    }

    public static implicit operator string(TranslateString str)
    {
        return str.ToString();
    }

#if NET8_0_OR_GREATER
    [GeneratedRegex(@"{.+?(?=})")]
    private static partial Regex ParameterRegex();
#endif
}
