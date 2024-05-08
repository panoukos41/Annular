# Annular Translate

Similar to [ngx-translate](https://github.com/ngx-translate/core) you will use the `TranslateService` to set and get translations and an `TranslateLoader`.

## TODO
- Params Interpolation

Needs to be developed furturh. There is a need to find a more efficient way that could also mimick the original interpolation of key/value instead of using index parameters with `string.Format`.

- TranslateCompiler

Needs to be defined and to create a default compiler. The default compiler should be implemented with the current `string.Format` implementations and later migrated to use the `Params Inerpolation` feature above (whichever comes first).

- TranslateServiceOptions

Fully implement the rest of the options.

- DefaultLang

Respect default lang for missing translations if set.

## Usage

By default, there is no loader available. You can add translations manually using `SetTranslation` but it is better to use a loader. You can write your own or use an existing one. For example you can use the `TranslateHttpLoader` that will load translations from remote json files using `HttpClient`.

Once you've decided which loader to use if any, you have to setup the `TranslateService` to use it. All arguments in the `TranslateService` are optional so it will fallback to defaults if something is not provided.

Here is how you would use the `TranslateHttpLoader` to load translations from "/i18n/[lang].json" ([lang] is the lang that you're using, for english it could be `en`):

```cs
services.AddScoped<TranslateLoader, TranslateHttpLoader>();
services.AddScoped<TranslateService>();
```

1. Init the `TranslateService` for your application

This can be done anywhere like `App.razor` or `MainLayout.razor` but `Program.cs` would be best as to not reload your UI for the first language again.

```cs
var app = builder.Build();

await InitializeLang(app);
await app.RunAsync();

// Custom code to get language from local storage and set it which will also download it.
static Task InitializeLang(WebAssemblyHost app)
{
    var localStorage = app.Services.GetRequiredService<ISyncLocalStorageService>();
    var translate = app.Services.GetRequiredService<TranslateService>();

    // add some languages to the list to use in the UI later. We can also filter the local storage one.
    translate.Langs.AddRange(["en", "el"]);

    // get saved lang from storage;
    var lang = localStorage.GetItem<string>("lang") ?? "en";

    // load current language. You can also call SetDefaultLang here.
    return translate.SetCurrentLang(lang).ToTask();
}
```

2. Define the translations

Once you've initialized `TranslateService`, you can put your translations in a json file that will be imported with the `TranslateHttpLoader`. The following translations should be stored in en.json.
```json
{
    "HELLO": "hello {0}"
}
```

You can also define your translations manually with setTranslation.
```cs
translate.SetTranslation("en", new Translations()
{
    { "HELLO", "hello {0}" }
});
```

The Translations dictionary understands nested JSON objects. This means that you can have a translation that looks like this (can be incorporated in the future on the service too.):
```cs
translate.SetTranslation("en", Translations.FromJsonDocument(JsonDocument.Parse("""
{
    "HOME": {
        "HELLO": "hello {0}"
    }
}
""")));
```
You can then access the value by using the dot notation, in this case `HOME.HELLO`.

It also understands arrays so you can have a translation like this

```cs
translate.SetTranslation("en", Translations.FromJsonDocument(JsonDocument.Parse("""
{
    "HOMES": [
        { "HELLO": "hello {0}" }
    ]
}
""")));
```
You then access the value by using the dot notation, in this case `HOMES.0.HELLO` and so on if you hade nested objc


3. Use the service

You use the `TranslateService` which also supports a "pipe" syntax to get your translation values.

With the service, it looks like this:
```cs
@inject TranslateService translate // or any way you want to inject it.

translate.GetTranslation("HELLO", "world").Subscribe((string res) => {
    Console.WriteLine(res);
    //=> 'hello world'
});
```

You can construct the translation keys dynamically by using simple string concatenation inside the html and using the "pipe":
```html
<ul *ngFor="let language of languages">
    @foreach (var language in languages)
    {
        <li>@("LANGUAGES." + language | translate)</li>
    }
</ul>
```
Where languages is an array member of your component:

```cs
string[] languages = ["EN", "FR", "BG"];
```

## Objects Description

`TranslateStore` is an object that stores translation related stuff like: Translations per language, available languages, current and default language and some subjects for notifications. The translate store does nothing by default and is managed by the `TranslateService`. If not provided the service will create one for itself and it won't be shared.

`TranslateServiceOptions` contains some options to control the service. As of now only the `DefaultLanguage` options is used.

`TranslateLoader` an abstract object to load translations. Implement this to load translations in a custom maner.

`TranslateHttpLoader` port of the the ngx-translate-http-loader. It uses settings `TranslateHttpLoaderOptions` with `Prefix` and `Suffix` in the combination `$"{Prefix}{lang}{Suffix}"` to find the json file to retrieve.
