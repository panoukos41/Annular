using System.Reactive.Linq;

namespace Annular.Translate;

public abstract class TranslateLoader
{
    public abstract IObservable<Translations> GetTranslation(string lang);
}

internal sealed class EmptyTranslateLoader : TranslateLoader
{
    public override IObservable<Translations> GetTranslation(string lang)
    {
        return Observable.Return(new Translations());
    }
}
