using System.Reactive.Linq;

namespace Annular.Translate.Defaults;

internal sealed class DefaultTranslateLoader : TranslateLoader
{
    public static DefaultTranslateLoader Instance { get; } = new();

    public override IObservable<Translations> GetTranslation(string lang)
    {
        return Observable.Return(new Translations());
    }
}
