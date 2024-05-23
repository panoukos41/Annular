using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Annular.Translate;

public class TranslateParameters : IReadOnlyDictionary<string, string>
{
    private InternalParameters dictionary;

    public int Count => dictionary.Count;

    public IEnumerable<string> Keys => dictionary.Keys;

    public IEnumerable<string> Values => dictionary.Values;

    public string this[string key]
    {
        get => TryGetValue(key, out var value) ? value : string.Empty;
        set => Set(key, value);
    }

    public TranslateParameters()
    {
        dictionary = InternalParameters.EmptyInstance;
    }

    public TranslateParameters(string key, string value)
    {
        dictionary = new InternalParameters.One(new(key, value));
    }

    public void Set(string key, string value)
    {
        dictionary = dictionary.Set(key, value);
    }

    public bool ContainsKey(string key)
    {
        return dictionary.TryGetValue(key, out _);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        return dictionary.TryGetValue(key, out value);
    }

    public bool Remove(string key)
    {
        dictionary = dictionary.TryRemove(key, out var success);
        return success;
    }

    public void Clear()
    {
        dictionary = InternalParameters.EmptyInstance;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return dictionary.GetEnumerator();
    }
}

internal abstract class InternalParameters : IEnumerable<KeyValuePair<string, string>>
{
    public static InternalParameters EmptyInstance { get; } = new Empty();

    public abstract int Count { get; }

    public abstract IEnumerable<string> Keys { get; }

    public abstract IEnumerable<string> Values { get; }

    public abstract InternalParameters Set(string key, string value);

    public abstract bool TryGetValue(string key, [NotNullWhen(true)] out string? value);

    public abstract bool TryGetNext(int index, out KeyValuePair<string, string> parameter);

    public abstract InternalParameters TryRemove(string key, out bool success);

    protected static bool TrySet(ref KeyValuePair<string, string> parameter, string key, string value)
    {
        if (parameter.Key == key)
        {
            parameter = new(key, value);
            return true;
        }
        return false;
    }

    protected static bool TryGet(ref KeyValuePair<string, string> parameter, string key, [NotNullWhen(true)] out string value)
    {
        Unsafe.SkipInit(out value);
        if (parameter.Key == key)
        {
            value = parameter.Value;
            return true;
        }
        return false;
    }

    protected static bool TryRemove(ref KeyValuePair<string, string> parameter, string key, out bool success)
    {
        return success = parameter.Key == key;
    }

    protected static bool TryGetNext(ref KeyValuePair<string, string> parameter, int at, int index, out KeyValuePair<string, string> value)
    {
        Unsafe.SkipInit(out value);
        if (at == index)
        {
            value = parameter;
            return true;
        }
        return false;
    }

    public virtual IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private struct Enumerator : IEnumerator<KeyValuePair<string, string>>
    {
        private readonly InternalParameters parameters;
        private int index = 0;
        private KeyValuePair<string, string> current = default;

        public Enumerator(InternalParameters parameters)
        {
            this.parameters = parameters;
        }

        public readonly KeyValuePair<string, string> Current => current;

        public bool MoveNext() => parameters.TryGetNext(index++, out current);

        public void Reset() => index = 0;

        readonly void IDisposable.Dispose()
        {
        }

        readonly object IEnumerator.Current => current;
    }

    // Empty
    private class Empty : InternalParameters
    {
        public override int Count { get; } = 0;

        public override IEnumerable<string> Keys => [];

        public override IEnumerable<string> Values => [];

        public override InternalParameters Set(string key, string value)
        {
            return new One(new(key, value));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            Unsafe.SkipInit(out value);
            return false;
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            success = false;
            return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            Unsafe.SkipInit(out parameter);
            return false;
        }
    }

    // One
    public class One : InternalParameters
    {
        private KeyValuePair<string, string> parameter1;

        public override int Count { get; } = 1;

        public override IEnumerable<string> Keys => [parameter1.Key];

        public override IEnumerable<string> Values => [parameter1.Value];

        public One(KeyValuePair<string, string> parameter)
        {
            parameter1 = parameter;
        }

        public override InternalParameters Set(string key, string value)
        {
            if (TrySet(ref parameter1, key, value))
            {
                return this;
            }
            return new Two(parameter1, new(key, value));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (TryGet(ref parameter1, key, out value))
            {
                return true;
            }
            return false;
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            if (TryRemove(ref parameter1, key, out success))
                return EmptyInstance;
            else
                return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            if (TryGetNext(ref parameter1, 0, index, out parameter))
            {
                return true;
            }
            return false;
        }
    }

    // Two
    private class Two : InternalParameters
    {
        private KeyValuePair<string, string> parameter1, parameter2;

        public override int Count { get; } = 2;

        public override IEnumerable<string> Keys => [parameter1.Key, parameter2.Key];

        public override IEnumerable<string> Values => [parameter1.Value, parameter2.Value];

        public Two(KeyValuePair<string, string> parameter1, KeyValuePair<string, string> parameter2)
        {
            this.parameter1 = parameter1;
            this.parameter2 = parameter2;
        }

        public override InternalParameters Set(string key, string value)
        {
            if (TrySet(ref parameter1, key, value) ||
                TrySet(ref parameter2, key, value))
            {
                return this;
            }
            return new Three(parameter1, parameter2, new(key, value));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (TryGet(ref parameter1, key, out value) ||
                TryGet(ref parameter2, key, out value))
            {
                return true;
            }
            return false;
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            if (TryRemove(ref parameter1, key, out success))
                return new One(parameter2);
            if (TryRemove(ref parameter2, key, out success))
                return new One(parameter1);
            else
                return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            if (TryGetNext(ref parameter1, 0, index, out parameter) ||
                TryGetNext(ref parameter2, 1, index, out parameter))
            {
                return true;
            }
            return false;
        }
    }

    // Three
    private class Three : InternalParameters
    {
        private KeyValuePair<string, string> parameter1, parameter2, parameter3;

        public override int Count { get; } = 3;

        public override IEnumerable<string> Keys => [parameter1.Key, parameter2.Key, parameter3.Key];

        public override IEnumerable<string> Values => [parameter1.Value, parameter2.Value, parameter3.Value];

        public Three(KeyValuePair<string, string> parameter1, KeyValuePair<string, string> parameter2, KeyValuePair<string, string> parameter3)
        {
            this.parameter1 = parameter1;
            this.parameter2 = parameter2;
            this.parameter3 = parameter3;
        }

        public override InternalParameters Set(string key, string value)
        {
            if (TrySet(ref parameter1, key, value) ||
                TrySet(ref parameter2, key, value) ||
                TrySet(ref parameter3, key, value))
            {
                return this;
            }
            return new Four(parameter1, parameter2, parameter3, new(key, value));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (TryGet(ref parameter1, key, out value) ||
                TryGet(ref parameter2, key, out value) ||
                TryGet(ref parameter3, key, out value))
            {
                return true;
            }
            return false;
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            if (TryRemove(ref parameter1, key, out success))
                return new Two(parameter2, parameter3);
            if (TryRemove(ref parameter2, key, out success))
                return new Two(parameter1, parameter3);
            if (TryRemove(ref parameter3, key, out success))
                return new Two(parameter1, parameter2);
            else
                return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            if (TryGetNext(ref parameter1, 0, index, out parameter) ||
                TryGetNext(ref parameter2, 1, index, out parameter) ||
                TryGetNext(ref parameter3, 2, index, out parameter))
            {
                return true;
            }
            return false;
        }
    }

    // Four
    private class Four : InternalParameters
    {
        private KeyValuePair<string, string> parameter1, parameter2, parameter3, parameter4;

        public override int Count { get; } = 4;

        public override IEnumerable<string> Keys => [parameter1.Key, parameter2.Key, parameter3.Key, parameter4.Key];

        public override IEnumerable<string> Values => [parameter1.Value, parameter2.Value, parameter3.Value, parameter4.Value];

        public Four(KeyValuePair<string, string> parameter1, KeyValuePair<string, string> parameter2, KeyValuePair<string, string> parameter3, KeyValuePair<string, string> parameter4)
        {
            this.parameter1 = parameter1;
            this.parameter2 = parameter2;
            this.parameter3 = parameter3;
            this.parameter4 = parameter4;
        }

        public override InternalParameters Set(string key, string value)
        {
            if (TrySet(ref parameter1, key, value) ||
                TrySet(ref parameter2, key, value) ||
                TrySet(ref parameter3, key, value) ||
                TrySet(ref parameter4, key, value))
            {
                return this;
            }
            return new Five(parameter1, parameter2, parameter3, parameter4, new(key, value));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (TryGet(ref parameter1, key, out value) ||
                TryGet(ref parameter2, key, out value) ||
                TryGet(ref parameter3, key, out value) ||
                TryGet(ref parameter4, key, out value))
            {
                return true;
            }
            return false;
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            if (TryRemove(ref parameter1, key, out success))
                return new Three(parameter2, parameter3, parameter4);
            if (TryRemove(ref parameter2, key, out success))
                return new Three(parameter1, parameter3, parameter4);
            if (TryRemove(ref parameter3, key, out success))
                return new Three(parameter1, parameter2, parameter4);
            if (TryRemove(ref parameter4, key, out success))
                return new Three(parameter1, parameter2, parameter3);
            else
                return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            if (TryGetNext(ref parameter1, 0, index, out parameter) ||
                TryGetNext(ref parameter2, 1, index, out parameter) ||
                TryGetNext(ref parameter3, 2, index, out parameter) ||
                TryGetNext(ref parameter4, 3, index, out parameter))
            {
                return true;
            }
            return false;
        }
    }

    // Five
    private class Five : InternalParameters
    {
        private KeyValuePair<string, string> parameter1, parameter2, parameter3, parameter4, parameter5;

        public override int Count { get; } = 5;

        public override IEnumerable<string> Keys => [parameter1.Key, parameter2.Key, parameter3.Key, parameter4.Key, parameter5.Key];

        public override IEnumerable<string> Values => [parameter1.Value, parameter2.Value, parameter3.Value, parameter4.Value, parameter5.Value];

        public Five(KeyValuePair<string, string> parameter1, KeyValuePair<string, string> parameter2, KeyValuePair<string, string> parameter3, KeyValuePair<string, string> parameter4, KeyValuePair<string, string> parameter5)
        {
            this.parameter1 = parameter1;
            this.parameter2 = parameter2;
            this.parameter3 = parameter3;
            this.parameter4 = parameter4;
            this.parameter5 = parameter5;
        }

        public override InternalParameters Set(string key, string value)
        {
            if (TrySet(ref parameter1, key, value) ||
                TrySet(ref parameter2, key, value) ||
                TrySet(ref parameter3, key, value) ||
                TrySet(ref parameter4, key, value) ||
                TrySet(ref parameter5, key, value))
            {
                return this;
            }
            return new Many(parameter1, parameter2, parameter3, parameter4, parameter5, new(key, value));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (TryGet(ref parameter1, key, out value) ||
                TryGet(ref parameter2, key, out value) ||
                TryGet(ref parameter3, key, out value) ||
                TryGet(ref parameter4, key, out value) ||
                TryGet(ref parameter5, key, out value))
            {
                return true;
            }
            return false;
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            if (TryRemove(ref parameter1, key, out success))
                return new Four(parameter2, parameter3, parameter4, parameter5);
            if (TryRemove(ref parameter2, key, out success))
                return new Four(parameter1, parameter3, parameter4, parameter5);
            if (TryRemove(ref parameter3, key, out success))
                return new Four(parameter1, parameter2, parameter4, parameter5);
            if (TryRemove(ref parameter4, key, out success))
                return new Four(parameter1, parameter2, parameter3, parameter5);
            if (TryRemove(ref parameter5, key, out success))
                return new Four(parameter1, parameter2, parameter3, parameter4);
            else
                return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            if (TryGetNext(ref parameter1, 0, index, out parameter) ||
                TryGetNext(ref parameter2, 1, index, out parameter) ||
                TryGetNext(ref parameter3, 2, index, out parameter) ||
                TryGetNext(ref parameter4, 3, index, out parameter) ||
                TryGetNext(ref parameter5, 4, index, out parameter))
            {
                return true;
            }
            return false;
        }
    }

    // Many
    private class Many : InternalParameters
    {
        private readonly Dictionary<string, string> parameters;

        public override int Count => parameters.Count;

        public override IEnumerable<string> Keys => parameters.Keys;

        public override IEnumerable<string> Values => parameters.Values;

        public Many(KeyValuePair<string, string> parameter1, KeyValuePair<string, string> parameter2, KeyValuePair<string, string> parameter3, KeyValuePair<string, string> parameter4, KeyValuePair<string, string> parameter5, KeyValuePair<string, string> parameter6)
        {
            parameters = new(6)
            {
                [parameter1.Key] = parameter1.Key,
                [parameter2.Key] = parameter2.Key,
                [parameter3.Key] = parameter3.Key,
                [parameter4.Key] = parameter4.Key,
                [parameter5.Key] = parameter5.Key,
                [parameter6.Key] = parameter6.Key,
            };
        }

        public override InternalParameters Set(string key, string value)
        {
            parameters[key] = value;
            return this;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return parameters.TryGetValue(key, out value);
        }

        public override InternalParameters TryRemove(string key, out bool success)
        {
            success = parameters.Remove(key);
            return this;
        }

        public override bool TryGetNext(int index, out KeyValuePair<string, string> parameter)
        {
            throw new NotSupportedException();
        }

        public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return parameters.GetEnumerator();
        }
    }
}
