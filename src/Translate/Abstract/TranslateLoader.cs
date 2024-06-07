using Annular.Translate.Primitives;

namespace Annular.Translate.Abstract;

public abstract class TranslateLoader
{
    public abstract IObservable<Translations> GetTranslation(string lang);
}
