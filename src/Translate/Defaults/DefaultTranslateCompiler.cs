using Annular.Translate.Abstract;
using Annular.Translate.Primitives;

namespace Annular.Translate.Defaults;

public sealed class DefaultTranslateCompiler : TranslateCompiler
{
    public static DefaultTranslateCompiler Instance { get; } = new();

    public override string Compile(string value, string lang)
    {
        return value;
    }

    public override Translations CompileTranslations(Translations translations, string lang)
    {
        return translations;
    }
}
