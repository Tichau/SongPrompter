using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Godot;

namespace Core.Services;

public class DependencyBuilder<TBase>
    where TBase : class
{
    private readonly string bindMethodName;
    private readonly Godot.Node? rootNode;

    private readonly List<InstanceMetadata> singletons = [];
    private bool built = false;

    public DependencyBuilder(string bindMethodName, Godot.Node? root)
    {
        this.bindMethodName = bindMethodName;
        this.rootNode = root;
    }

    public DependencyBuilder<TBase> AddSingleton<TImplementation>(PackedScene scene)
        where TImplementation : TBase
    {
        return this.AddSingleton<TImplementation, TImplementation>(scene);
    }

    public DependencyBuilder<TBase> AddSingleton<TImplementation>(TImplementation instance)
        where TImplementation : TBase
    {
        return this.AddSingleton<TImplementation, TImplementation>(instance);
    }

    public DependencyBuilder<TBase> AddSingleton<TImplementation>()
        where TImplementation : class, TBase, new()
    {
        return this.AddSingleton<TImplementation, TImplementation>();
    }

    public DependencyBuilder<TBase> AddSingleton<TService, TImplementation>()
        where TImplementation : class, TBase, TService, new()
    {
        TImplementation? instance = default;
        Type service = typeof(TService);
        int singletonIndex = this.singletons.FindIndex(metadata => metadata.Instance != null && metadata.Instance.GetType() == typeof(TImplementation));
        if (singletonIndex >= 0)
        {
            // An instance of the same singleton type already exists. Share the instances.
            instance = this.singletons[singletonIndex].Instance as TImplementation;
        }
        else
        {
            instance = new();
        }

        Debug.Assert(instance != null);

        return this.AddSingleton<TService, TImplementation>(instance);
    }

    public DependencyBuilder<TBase> AddSingleton<TService, TImplementation>(TImplementation instance)
        where TImplementation : TBase, TService
    {
        Type service = typeof(TService);
        if (this.singletons.Any(metadata => metadata.Service == service))
        {
            throw new ArgumentException($"Service {service} already registered.");
        }

        Type type = typeof(TImplementation);
        if (type.IsAbstract)
        {
            throw new ArgumentException("Singleton type should not be abstract.");
        }

        MethodInfo? bindMethod = type.GetMethod(this.bindMethodName);
        if (bindMethod == null)
        {
            throw new ArgumentException($"Singleton {type} should implement '{this.bindMethodName}' method.");
        }

        this.singletons.Add(new InstanceMetadata(service, type, instance, bindMethod));
        return this;
    }

    public DependencyBuilder<TBase> AddSingleton<TService, TImplementation>(PackedScene scene)
        where TImplementation : TBase, TService
    {
        if (this.rootNode == null)
        {
            throw new Exception("A root node must be set to add a scene singleton.");
        }

        Type service = typeof(TService);
        if (this.singletons.Any(metadata => metadata.Service == service))
        {
            throw new ArgumentException($"Service {service} already registered.");
        }

        Type type = typeof(TImplementation);
        if (type.IsAbstract)
        {
            throw new ArgumentException("Singleton type should not be abstract.");
        }

        MethodInfo? bindMethod = type.GetMethod(this.bindMethodName);
        if (bindMethod == null)
        {
            throw new ArgumentException($"Singleton type should implement '{this.bindMethodName}' method.");
        }

        this.singletons.Add(new InstanceMetadata(service, type, scene, bindMethod));
        return this;
    }

    public Collection<TBase> Build()
    {
        Debug.Assert(!this.built);

        List<(Type service, TBase instance)> instances = [];
        DependencyBuilder<TBase>.InstanceMetadata[] singletons = this.singletons.ToArray();

        // Instantiate all singletons.
        for (int i = 0; i < singletons.Length; i++)
        {
            ref InstanceMetadata metadata = ref singletons[i];
            if (metadata.Instance == null)
            {
                Debug.Assert(metadata.Scene != null);
                TBase node = metadata.Scene.Instantiate<TBase>();
                Debug.Assert(node != null && node is Godot.Node);
                Debug.Assert(this.rootNode != null);
                this.rootNode.AddChild(node as Godot.Node);
                metadata.Instance = node;
            }
        }

        // Bind services.
        int count = 0;
        while (instances.Count < this.singletons.Count)
        {
            if (count > this.singletons.Count)
            {
                throw new Exception("Cycle detected in singleton dependencies.");
            }

            for (int i = 0; i < singletons.Length; i++)
            {
                InstanceMetadata metadata = singletons[i];
                if (instances.Any(pair => pair.service == metadata.Service))
                {
                    // Service already registered.
                    continue;
                }

                // Try to bind service.
                for (int index = 0; index < metadata.Parameters.Length; index++)
                {
                    metadata.Parameters[index] = instances.FirstOrDefault(pair => pair.service == metadata.Dependencies[index]).instance;
                }

                if (metadata.Parameters.All(param => param != null))
                {
                    try
                    {
                        if (metadata.Service != metadata.Implementation)
                        {
                            Trace.WriteLine($"Bind service {metadata.Service} on singleton {metadata.Implementation}...");
                        }
                        else
                        {
                            Trace.WriteLine($"Bind service {metadata.Service}...");
                        }

                        Debug.Assert(metadata.Instance != null);
                        metadata.BindMethod.Invoke(metadata.Instance, metadata.Parameters);

                        instances.Add((metadata.Service, metadata.Instance));
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError($"Exception thrown during service binding: {exception}");
                    }
                }
            }

            count++;
        }

        this.built = true;
        Trace.WriteLine($"Dependencies built in {count} pass(es)");

        return new Collection<TBase>(instances);
    }

    private struct InstanceMetadata
    {
        public readonly Type Service;
        public readonly Type Implementation;
        public readonly MethodInfo BindMethod;
        public readonly Type[] Dependencies;
        public readonly object?[] Parameters;

        // Scene or instance:
        public PackedScene? Scene;
        public TBase? Instance;

        public InstanceMetadata(Type service, Type implementation, PackedScene scene, MethodInfo bindMethod) : this()
        {
            this.Service = service;
            this.Implementation = implementation;
            this.BindMethod = bindMethod;
            this.Dependencies = bindMethod.GetParameters().Select(param => param.ParameterType).ToArray();
            this.Parameters = new object[this.Dependencies.Length];
            this.Scene = scene;
        }

        public InstanceMetadata(Type service, Type implementation, TBase instance, MethodInfo bindMethod) : this()
        {
            this.Service = service;
            this.Implementation = implementation;
            this.BindMethod = bindMethod;
            this.Dependencies = bindMethod.GetParameters().Select(param => param.ParameterType).ToArray();
            this.Parameters = new object[this.Dependencies.Length];
            this.Instance = instance;
        }
    }
}
