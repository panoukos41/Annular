using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Text.Json;
using Annular.Translate.Abstract;
using Annular.Translate.Primitives;

namespace Annular.Translate.HttpLoader;

public sealed class TranslateHttpLoader : TranslateLoader
{
    private readonly HttpClient httpClient;
    private readonly TranslateHttpLoaderOptions options;

    public TranslateHttpLoader(HttpClient httpClient, TranslateHttpLoaderOptions? options = null)
    {
        this.httpClient = httpClient;
        this.options = options ?? new();
    }

    /// <summary>
    /// Gets the translations from the server
    /// </summary>
    public override IObservable<Translations> GetTranslation(string lang)
    {
        return Observable
            .FromAsync(token => httpClient.GetFromJsonAsync<JsonDocument>($"{options.Prefix}{lang}{options.Suffix}", token))
            .Select(Translations.FromJsonDocument);
    }
}
