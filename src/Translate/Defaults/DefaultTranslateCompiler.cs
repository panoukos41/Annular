namespace Annular.Translate.Defaults;

internal sealed class DefaultTranslateCompiler : TranslateCompiler
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
