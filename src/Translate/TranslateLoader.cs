namespace Annular.Translate;

public abstract class TranslateLoader
{
    public abstract IObservable<Translations> GetTranslation(string lang);
}
