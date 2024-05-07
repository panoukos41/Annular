namespace Annular.Translate;

public class TranslateServiceOptions
{
    /// <summary>
    /// Whether we should use default language translation when current language translation is missing.
    /// </summary>
    public bool UseDefaultLang { get; set; } = true;

    /// <summary>
    /// Whether this service should use the store or not.
    /// </summary>
    public bool Isolate { get; set; }

    /// <summary>
    /// To make a child module extend (and use) translations from parent modules.
    /// </summary>
    public bool Extend { get; set; }

    /// <summary>
    /// Set the default language using configuration.
    /// </summary>
    public string DefaultLanguage { get; set; } = string.Empty;
}
