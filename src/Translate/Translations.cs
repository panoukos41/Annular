using System.Text.Json;

namespace Annular.Translate;

public sealed class Translations : Dictionary<string, string>
{
    public new string this[string key]
    {
        get => ContainsKey(key) ? base[key] : key;
        set => base[key] = value;
    }

    /// <summary>
    /// Merge another dictionary to this.
    /// This will replace existing if they exist in both dictionaries.
    /// </summary>
    public void Merge(IDictionary<string, string> values)
    {
        foreach (var (k, v) in values)
        {
            this[k] = v;
        }
    }

    /// <summary>
    /// Returns the parsed result of the translation for a given key.
    /// </summary>
    public TranslateString GetParsedResult(string key, TranslateParameters? parameters = null)
    {
        return new(this[key], parameters);
    }

    public static TranslateString operator |(string key, Translations translations)
    {
        return translations.GetParsedResult(key);
    }

    /// <summary>
    /// Create a <see cref="Translations"/> object from a <see cref="JsonDocument"/>.
    /// </summary>
    public static Translations FromJsonDocument(JsonDocument? json)
    {
        var translations = new Translations();

        return json?.RootElement.ValueKind switch
        {
            JsonValueKind.Object => FromObject(translations, string.Empty, json.RootElement),
            JsonValueKind.Array => FromArray(translations, string.Empty, json.RootElement),
            _ => translations
        };

        static Translations FromObject(Translations translations, string parentName, JsonElement element)
        {
            foreach (var obj in element.EnumerateObject())
            {
                var name = $"{parentName}{obj.Name}";
                if (obj.Value.ValueKind is JsonValueKind.Object)
                {
                    FromObject(translations, $"{name}.", obj.Value);
                }
                else if (obj.Value.ValueKind is JsonValueKind.Array)
                {
                    FromArray(translations, $"{name}.", obj.Value);
                }
                else if (obj.Value.ValueKind is not JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    translations[name] = obj.Value.ToString();
                }
            }
            return translations;
        }

        static Translations FromArray(Translations translations, string parentName, JsonElement element)
        {
            var index = 0;
            foreach (var obj in element.EnumerateArray())
            {
                var name = $"{parentName}{index}";
                if (obj.ValueKind is JsonValueKind.Object)
                {
                    FromObject(translations, $"{name}.", obj);
                }
                else if (obj.ValueKind is JsonValueKind.Array)
                {
                    FromArray(translations, $"{name}.", obj);
                }
                else if (obj.ValueKind is not JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    translations[name] = obj.ToString();
                }
                index++;
            }
            return translations;
        }
    }
}
