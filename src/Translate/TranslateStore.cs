using System.Reactive.Subjects;
using Annular.Translate.Events;
using Annular.Translate.Primitives;

namespace Annular.Translate;

public class TranslateStore
{
    public TranslateStore()
    {
        DefaultLang = CurrentLang = string.Empty;
    }

    public TranslateStore(string defaultLang)
    {
        DefaultLang = CurrentLang = defaultLang;
        Langs.Add(DefaultLang);
    }

    /// <summary>
    /// The default lang to fallback when translations are missing on the current lang.
    /// </summary>
    public string DefaultLang { get; set; }

    /// <summary>
    /// The lang currently used.
    /// </summary>
    public string CurrentLang { get; set; }

    /// <summary>
    /// A list of translations per lang.
    /// </summary>
    public TranslationsDictionary Translations { get; set; } = [];

    /// <summary>
    /// A list of available languages.
    /// </summary>
    public HashSet<string> Langs { get; } = [];

    /// <summary>
    /// A subject to listen for translation change events.
    /// </summary>
    public Subject<TranslationChangeEvent> OnTranslationChange { get; } = new();

    /// <summary>
    /// A subject to listen for lang change events.
    /// </summary>
    public Subject<TranslationChangeEvent> OnLangChange { get; } = new();

    /// <summary>
    /// A subject to listen for default lang change events.</summary
    /// >summary>
    public Subject<TranslationChangeEvent> OnDefaultLangChange { get; } = new();
}
