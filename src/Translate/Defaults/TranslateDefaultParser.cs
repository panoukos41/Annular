﻿using System.Text;
using Annular.Translate.Abstract;
using Annular.Translate.Primitives;

namespace Annular.Translate.Defaults;

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
