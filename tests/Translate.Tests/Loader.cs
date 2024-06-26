using Annular.Translate.Abstract;
using Annular.Translate.Primitives;
using FluentAssertions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Annular.Translate.Tests;

public sealed class Loader
{
    // Simulates loader like http where it could take some time to load translations.
    private class MockLoader : TranslateLoader
    {
        private static readonly Translations translations = [];
        private TaskCompletionSource tsc = new();

        public int Count { get; private set; }

        public void CompleteLoad() => tsc.TrySetResult();

        public void Reset()
        {
            tsc = new();
            Count = 0;
        }

        public override IObservable<Translations> GetTranslation(string lang) => Observable.Create<Translations>(async o =>
        {
            Count++;
            await tsc.Task;
            o.OnNext(translations);
            o.OnCompleted();
            return Disposable.Empty;
        });
    }

    private readonly MockLoader loader;
    private readonly TranslateService translate;
    private int langChangeCount;

    public Loader()
    {
        loader = new MockLoader();
        translate = new TranslateService(loader: loader);
    }

    // Helper method to call methods multiple time and return a task that completes when all tasks complete
    private static Task<T[]> ParallelWhenAll<T>(int iterations, Func<int, Task<T>> task)
    {
        var tasks = new Task<T>[iterations];

        Parallel.For(0, iterations, index => tasks[index] = task(index));

        return Task.WhenAll(tasks);
    }

    // All below methods interact with the loader. No matter how many times they get executed
    // if the loader is has not finished loading it should always return the same one.
    // some methods should actually after the first loader event should always return the cached 
    // result this is something not being tested for now.

    [Fact]
    public async Task Load_Should_Be_Called_Once()
    {
        var all = ParallelWhenAll(10_000, _ => translate.LoadTranslation("el").ToTask());

        loader.CompleteLoad();
        await all;

        loader.Count.Should().Be(1);
    }

    [Fact]
    public async Task Set_Default_Should_Load_Once()
    {
        translate.OnDefaultLangChange.Subscribe(_ => Interlocked.Increment(ref langChangeCount));
        var all = ParallelWhenAll(10_000, _ => translate.SetDefaultLang("el").ToTask());

        loader.CompleteLoad();
        await all;

        loader.Count.Should().Be(1);
        langChangeCount.Should().Be(1);
    }

    [Fact]
    public async Task Set_Current_Should_Load_Once()
    {
        translate.OnLangChange.Subscribe(_ => Interlocked.Increment(ref langChangeCount));
        var all = ParallelWhenAll(10_000, _ => translate.SetCurrentLang("el").ToTask());

        loader.CompleteLoad();
        await all;

        loader.Count.Should().Be(1);
        langChangeCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_Should_Be_Called_Once()
    {
        var all = ParallelWhenAll(10_000, _ => translate.Get("test.key").ToTask());

        loader.CompleteLoad();
        await all;

        loader.Count.Should().Be(1);
    }
}
