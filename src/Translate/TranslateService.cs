using Annular.Translate.Abstract;
using Annular.Translate.Defaults;
using Annular.Translate.Events;
using Annular.Translate.Primitives;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Annular.Translate;

public class TranslateService
{
    private readonly TranslateStore store;
    private readonly TranslateLoader loader;
    private readonly TranslateParser parser;
    private readonly TranslateCompiler compiler;
    private readonly TranslateServiceOptions options;

    private readonly Dictionary<string, IObservable<Translations>> translationRequests = [];
    private readonly Dictionary<string, IObservable<Translations>> translationsLoading = [];

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
    /// Initialize a new 
    /// </summary>
    /// <param name="store">An instance of the store (that is supposed to be unique).</param>
    /// <param name="loader">An instance of the loader to use.</param>
    /// <param name="parser">An instance of the parser currently used</param>
    /// <param name="compiler">An instance of the compiler currently used</param>
    /// <param name="options">Options to configure the current service.</param>
    public TranslateService(
        TranslateStore? store = null,
        TranslateLoader? loader = null,
        TranslateParser? parser = null,
        TranslateCompiler? compiler = null,
        TranslateServiceOptions? options = null)
    {
        this.store = store ?? new();
        this.loader = loader ?? DefaultTranslateLoader.Instance;
        this.parser = parser ?? TranslateDefaultParser.Instance;
        this.compiler = compiler ?? DefaultTranslateCompiler.Instance;
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
        if (store.Translations.ContainsKey(lang) is false &&
            translationRequests.TryGetValue(lang, out pending) is false)
        {
            translationRequests[lang] = pending = LoadTranslation(lang);
        }
        return pending;
    }

    /// <summary>
    /// Gets translations for a given language with the current loader.
    /// </summary>
    /// <param name="lang">The lang to load.</param>
    /// <param name="merge">Whether to merge with the the current translations or replace them.</param>
    /// <remarks>
    /// If there is already a loading request it will be returned.
    /// You can call <see cref="ResetLang(string)"/> to cancel it and load again.
    /// </remarks>
    public IObservable<Translations> LoadTranslation(string lang, bool merge = false)
    {
        Langs.Add(lang);

        if (translationsLoading.TryGetValue(lang, out var pending) is false)
        {
            translationsLoading[lang] = pending = loader
                .GetTranslation(lang)
                .Do(translations =>
                {
                    if (merge)
                        store.Translations[lang].Merge(translations);
                    else
                        store.Translations[lang] = translations;

                    translationsLoading.Remove(lang);
                })
                .Catch<Translations, Exception>(Observable.Throw<Translations>)
                .Replay()
                .AutoConnect()
                .Take(1);
        }
        return pending;


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
        store.OnTranslationChange.OnNext(new(lang, store.Translations[lang]));
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
    /// <param name="lang">The lang to re-load.</param>
    /// <param name="merge">Whether to merge with the the current translations or replace them.</param>
    public IObservable<Translations> ReloadLang(string lang, bool merge = false)
    {
        ResetLang(lang);
        translationsLoading.Remove(lang);
        return LoadTranslation(lang, merge);
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
    public IObservable<string> Get(string key, TranslateParameters? parameters = null)
    {
        // todo: Implement array version.
        if (RetrieveTranslations(CurrentLang) is { } pending)
        {
            return pending.Take(1).Select(translations => translations.GetParsedResult(key, parameters).ToString());
        }

        var r = store.Translations[CurrentLang].GetParsedResult(key, parameters).ToString();
        return Observable.Return(r);
    }

    /// <summary>
    /// Returns a translation instantly from the internal state of loaded translation.
    /// All rules regarding the current language, the preferred language of even fallback languages will be used except any promise handling.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public string Instant(string key, TranslateParameters? parameters)
    {
        return store.Translations[CurrentLang].GetParsedResult(key, parameters);
    }

    public static TranslateString operator |(string key, TranslateService service)
    {
        return service.Translations[service.CurrentLang].GetParsedResult(key);
    }
}
