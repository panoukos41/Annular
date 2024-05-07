using System.Buffers;

namespace Annular.Translate;

public readonly ref struct TranslateString
{
    private static readonly SearchValues<char> possibleFormat = SearchValues.Create(['{', '}']);

    private readonly string value;
    private readonly List<string?>? interpolateParams;

    public TranslateString(string value, List<string?>? interpolateParams = null)
    {
        this.value = value;
        this.interpolateParams = interpolateParams;
    }

    public override string ToString()
    {
        if (interpolateParams?.Count is null or 0) return value;

        // todo: Find way to improve this.
        return string.Format(value, [.. interpolateParams]);
    }

    public TranslateString Add(string arg)
    {
        if (value.AsSpan().IndexOfAny(possibleFormat) is -1) return this;

        var @params = interpolateParams ?? [];
        @params.Add(arg);
        return new(value, @params);
    }

    public TranslateString Add(params string[] args)
    {
        if (value.AsSpan().IndexOfAny(possibleFormat) is -1) return this;

        var @params = interpolateParams ?? [];
        @params.AddRange(args);
        return new(value, @params);
    }

    public static TranslateString operator |(TranslateString str, string param)
    {
        return str.Add(param);
    }

    public static implicit operator string(TranslateString str)
    {
        return str.ToString();
    }
}
