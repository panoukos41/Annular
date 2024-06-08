using System.Reactive.Linq;
using Annular.Translate.Abstract;
using Annular.Translate.Primitives;

namespace Annular.Translate.Defaults;

public sealed class DefaultTranslateLoader : TranslateLoader
{
    public static DefaultTranslateLoader Instance { get; } = new();

    public override IObservable<Translations> GetTranslation(string lang)
    {
        return Observable.Return(new Translations());
    }
}
