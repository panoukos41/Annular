using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Annular.Translate;

public class TranslateService
{
    private readonly TranslateStore store;
    private readonly TranslateLoader loader;
    private readonly TranslateServiceOptions options;

    private readonly Dictionary<string, IObservable<Translations>> translationRequests = [];
    private IObservable<Translations>? loadingTranslations;
    private bool pending;

    /// <summary>
    /// The default lang to fallback when translations are missing on the current lang.
    /// </summary>
    public string DefaultLang => store.DefaultLang;

    /// <summary>
    /// The lang currently used.
    /// </summary>
    public string CurrentLang => store.CurrentLang;

    /// <summary>
    /// A list of translations per lang.
    /// </summary>
    public TranslationsDictionary Translations => store.Translations;

    /// <summary>
    /// Translations for the default lang.
    /// </summary>
    public Translations Default => Translations[DefaultLang];

    /// <summary>
    /// Translations for the current lang.
    /// </summary>
    public Translations Current => Translations[CurrentLang];

    /// <summary>
    /// An list of available languages.
    /// </summary>
    public HashSet<string> Langs => store.Langs;

    /// <summary>
    /// A subject to listen to translation change events.
    /// </summary>
    public Subject<TranslationChangeEvent> OnTranslationChange => store.OnTranslationChange;

    /// <summary>
    /// A subject to listen to lang change events.
    /// </summary>
    public Subject<TranslationChangeEvent> OnLangChange => store.OnLangChange;

    /// <summary>
    /// A subject to listen to default lang change events.</summary
    /// >summary>
    public Subject<TranslationChangeEvent> OnDefaultLangChange => store.OnDefaultLangChange;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loader">An instance of the loader to use.</param>
    /// <param name="store">An instance of the store (that is supposed to be unique).</param>
    /// <param name="options">Options to configure the current service.</param>
    public TranslateService(TranslateLoader loader, TranslateStore? store = null, TranslateServiceOptions? options = null)
    {
        this.loader = loader;
        this.store = store ?? new();
        this.options = options ?? new();

        if (!string.IsNullOrEmpty(this.options.DefaultLanguage))
        {
            this.store.DefaultLang = this.options.DefaultLanguage;
        }
    }

    /// <summary>
    /// Sets the default language to use as a fallback.
    /// </summary>
    public IObservable<Translations> SetDefaultLang(string lang)
    {
        if (lang == DefaultLang && store.Translations.TryGetValue(lang, out Translations? value))
        {
            return Observable.Return(value);
        }
        if (RetrieveTranslations(lang) is { } pending)
        {
            // on init set the defaultLang immediately
            if (string.IsNullOrEmpty(DefaultLang))
            {
                store.DefaultLang = lang;
            }
            pending.Take(1).Subscribe(res =>
            {
                ChangeDefaultLang(lang);
            });
            return pending;
        }
        // we already have this language
        ChangeDefaultLang(lang);
        return Observable.Return(store.Translations[lang]);
    }

    /// <summary>
    /// Sets the language to use.
    /// </summary>
    public IObservable<Translations> SetCurrentLang(string lang)
    {
        if (lang == CurrentLang && store.Translations.TryGetValue(lang, out Translations? value))
        {
            return Observable.Return(value);
        }
        if (RetrieveTranslations(lang) is { } pending)
        {
            if (string.IsNullOrEmpty(CurrentLang))
            {
                store.CurrentLang = lang;
            }
            pending.Take(1).Subscribe(translations =>
            {
                ChangeLang(lang);
            });
            return pending;
        }
        // we already have this language
        ChangeLang(lang);
        return Observable.Return(store.Translations[lang]);
    }

    /// <summary>
    /// Retrieves the given translations
    /// </summary>
    private IObservable<Translations>? RetrieveTranslations(string lang)
    {
        IObservable<Translations>? pending = null;

        // if this language is unavailable ask for it.
        if (!store.Translations.ContainsKey(lang) && !translationRequests.TryGetValue(lang, out pending))
        {
            translationRequests[lang] = pending = GetTranslation(lang);
        }
        return pending;
    }

    /// <summary>
    /// Gets translations for a given language with the current loader.
    /// </summary>
    /// <param name="lang">The lang to load.</param>
    /// <param name="merge">Whether to merge the new translations to the currently loaded translations.</param>
    public IObservable<Translations> GetTranslation(string lang, bool merge = false)
    {
        pending = true;
        Langs.Add(lang);
        return loadingTranslations = loader
            .GetTranslation(lang)
            .Do(translations =>
            {
                store.Translations[lang] = translations; // todo: Implement merge
                pending = false;
            })
            .Catch<Translations, Exception>(ex =>
            {
                pending = false;
                return Observable.Throw<Translations>(ex);
            })
            .Replay()
            .AutoConnect()
            .Take(1);
    }

    /// <summary>
    /// Manually sets an object of translations for a given language.
    /// </summary>
    public void SetTranslation(string lang, Translations translations, bool shouldMerge = false)
    {
        Langs.Add(lang);
        if (shouldMerge)
            store.Translations[lang].Merge(translations);
        else
            store.Translations[lang] = translations;
        store.OnTranslationChange.OnNext(new(lang, translations));
    }

    /// <summary>
    /// Add available langs
    /// </summary>
    public void AddLangs(params string[] langs)
    {
        foreach (string lang in langs)
        {
            Langs.Add(lang);
        }
    }

    /// <summary>
    /// Sets the translated value of a key.
    /// </summary>
    public void Set(string key, string value, string? lang = null)
    {
        lang ??= CurrentLang;
        var translations = store.Translations[lang];
        translations[key] = value;
        OnTranslationChange.OnNext(new(lang, translations));
    }

    /// <summary>
    /// Changes the current lang
    /// </summary>
    private void ChangeLang(string lang)
    {
        store.CurrentLang = lang;
        store.OnLangChange.OnNext(new(lang, store.Translations[lang]));
        // if there is no default lang, use the one that we just set
        if (string.IsNullOrEmpty(DefaultLang))
        {
            ChangeDefaultLang(lang);
        }
    }

    /// <summary>
    /// Changes the default lang
    /// </summary>
    private void ChangeDefaultLang(string lang)
    {
        store.DefaultLang = lang;
        store.OnDefaultLangChange.OnNext(new(lang, store.Translations[lang]));
        // if there is no current lang, use the one that we just set
        if (string.IsNullOrEmpty(CurrentLang))
        {
            ChangeLang(lang);
        }
    }

    /// <summary>
    /// Reloads the provided <paramref name="lang"/>.
    /// </summary>
    public IObservable<Translations> ReloadLang(string lang)
    {
        ResetLang(lang);
        return GetTranslation(lang);
    }

    /// <summary>
    /// Deletes inner translations For provided <paramref name="lang"/>.
    /// </summary>
    public void ResetLang(string lang)
    {
        translationRequests.Remove(lang);
        store.Translations[lang].Clear();
    }

    /// <summary>
    /// Gets the translated value of a key (or an array of keys)
    /// </summary>
    /// <returns>The translated key.</returns>
    public IObservable<string> Get(string key, params string?[] interpolateParams)
    {
        // todo: Implement array version.
        // check if we are loading a new translation to use
        if (pending && loadingTranslations is { })
        {
            return loadingTranslations.Select(translations => translations.GetParsedResult(key, interpolateParams).ToString());
        }
        else
        {
            var r = store.Translations[CurrentLang].GetParsedResult(key, interpolateParams).ToString();
            return Observable.Return(r);
        }
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translation.
    /// All rules regarding the current language, the preferred language of even fallback languages will be used except any promise handling.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="interpolateParams"></param>
    /// <returns></returns>
    public string Instant(string key, params string?[] interpolateParams)
    {
        return store.Translations[CurrentLang].GetParsedResult(key, interpolateParams);
    }

    public static TranslateString operator |(string key, TranslateService service)
    {
        return service.Translations[service.CurrentLang].GetParsedResult(key);
    }
}
