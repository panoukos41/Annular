namespace Annular.Translate.Primitives;

public sealed class TranslationsDictionary : Dictionary<string, Translations>
{
    public new Translations this[string key]
    {
        get => ContainsKey(key) ? base[key] : base[key] = [];
        set => base[key] = value;
    }
}
