using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Core.Services;

public class Collection<TBase> : IEnumerable<TBase>
    where TBase : class
{
    private readonly Dictionary<Type, uint> instanceIds = [];
    private readonly TBase[] instances;

    public Collection(IEnumerable<(Type service, TBase instance)> instances)
    {
        List<TBase> instanceList = [];
        foreach ((Type service, TBase instance) pair in instances)
        {
            int index = instanceList.FindIndex(instance => instance == pair.instance);
            if (index < 0)
            {
                instanceList.Add(pair.instance);
                index = instanceList.Count - 1;
            }

            Debug.Assert(index >= 0);
            this.instanceIds.Add(pair.service, (uint)index);
        }

        this.instances = instanceList.ToArray();
    }

    public IEnumerator<TBase> GetEnumerator() => ((IEnumerable<TBase>)this.instances).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.instances.GetEnumerator();

    public T Get<T>()
    {
        if (!this.instanceIds.TryGetValue(typeof(T), out uint instanceId))
        {
            throw new Exception($"No instance registered for type {typeof(T)}.");
        }

        var instance = (T)(object)this.instances[instanceId];
        return instance;
    }

    public T? GetOrDefault<T>()
    {
        if (!this.instanceIds.TryGetValue(typeof(T), out uint instanceId))
        {
            return default;
        }

        var instance = (T)(object)this.instances[instanceId];
        return instance;
    }
}
