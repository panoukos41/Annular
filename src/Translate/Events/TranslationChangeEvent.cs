using Annular.Translate.Primitives;

namespace Annular.Translate.Events;

public class TranslationChangeEvent : EventArgs
{
    public string Lang { get; }

    public Translations Translations { get; }

    public TranslationChangeEvent(string lang, Translations translations)
    {
        Lang = lang;
        Translations = translations;
    }
}
