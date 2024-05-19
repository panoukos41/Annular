namespace Annular.Translate;

public abstract class TranslateCompiler
{
    public abstract string Compile(string value, string lang);

    public abstract Translations CompileTranslations(Translations translations, string lang);
}
