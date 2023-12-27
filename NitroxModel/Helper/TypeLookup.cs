using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NitroxModel.Helper;

/// TODO: Convert this API to a source generator to get compile-time type safety and a performance boost.
/// <summary>
///     Type lookup helper for finding implementations that are compatible for the given <see cref="TBaseInterface" />. The
///     given wrapper is used on every implementing type so that they are compatible with the first generic
///     parameter of the given <see cref="TBaseInterface" />.
/// </summary>
/// <typeparam name="TBaseInterface">
///     The common generic interface to use as key value in the lookup. All implementors must
///     be compatible with this key for them to be found and instantiated.
/// </typeparam>
public sealed class TypeLookup<TBaseInterface> : IReadOnlyDictionary<Type, TBaseInterface>
{
    private readonly Dictionary<Type, TBaseInterface> lookup;

    public int Count => lookup.Count;

    public TBaseInterface this[Type key] => lookup[key];

    public IEnumerable<Type> Keys => lookup.Keys;
    public IEnumerable<TBaseInterface> Values => lookup.Values;

    private TypeLookup(Dictionary<Type, TBaseInterface> lookup)
    {
        this.lookup = lookup;
    }

    public static TypeLookup<TBaseInterface> Create<TWrapper>(IEnumerable<Type> types, Func<Type, object> ctorArgumentTypeResolver = null) // where TWrapper : TBaseInterface
    {
        static IEnumerable<(Type ImplementingType, Type[] AssignableInterfaces)> FilterSuitable(IEnumerable<Type> types, Type wrapperImplementingInterfaceDef, Type wrapperTypeDef)
        {
            foreach (Type type in types.Where(t => !t.IsAbstract && !t.IsInterface))
            {
                if (type == wrapperTypeDef)
                {
                    continue;
                }
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Length == 0)
                {
                    continue;
                }
                Type[] suitableInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == wrapperImplementingInterfaceDef).ToArray();
                if (suitableInterfaces.Length == 0)
                {
                    continue;
                }

                yield return (type, suitableInterfaces);
            }
        }

        static IEnumerable<object> ResolveTypesToInstances(IEnumerable<Type> types, Dictionary<Type, object> previouslyCreatedInstances, Func<Type, object> unknownTypeResolver)
        {
            foreach (Type type in types)
            {
                if (previouslyCreatedInstances.TryGetValue(type, out object instance))
                {
                    yield return instance;
                    continue;
                }

                object resolvedType = unknownTypeResolver?.Invoke(type);
                if (resolvedType != null)
                {
                    yield return resolvedType;
                    continue;
                }

                throw new Exception($"Unable to resolve type {type}");
            }
        }

        // Record types have IEquatable that we should ignore.
        Type[] wrapperInterfaces = typeof(TWrapper).GetInterfaces().Where(i => i.GetGenericTypeDefinition() != typeof(IEquatable<>)).ToArray();
        Type wrapperImplementingInterfaceType = wrapperInterfaces.FirstOrDefault();
        if (wrapperImplementingInterfaceType == null || wrapperInterfaces.Length != 1)
        {
            throw new ArgumentException("wrapper type must have exactly one generic interface", nameof(TWrapper));
        }
        if (!wrapperImplementingInterfaceType.IsInterface)
        {
            throw new ArgumentException("must be interface", nameof(TWrapper));
        }
        if (wrapperImplementingInterfaceType.GenericTypeArguments.Length != 1)
        {
            throw new ArgumentException("must be generic interface with a single argument");
        }

        Type wrapperGenericTypeDef = typeof(TWrapper).GetGenericTypeDefinition();
        Dictionary<Type, object> previouslyCreatedInstances = new();
        Dictionary<Type, TBaseInterface> lookup = new();
        foreach ((Type ImplementingType, Type[] AssignableInterfaces) typeInfo in FilterSuitable(types, wrapperImplementingInterfaceType.GetGenericTypeDefinition(), wrapperGenericTypeDef)
                     .OrderBy(entry => entry.ImplementingType.GetConstructors().Max(c => c.GetParameters().Length)))
        {
            var ctor = typeInfo.ImplementingType.GetConstructors().Select(c => new { Ctor = c, Params = c.GetParameters() }).OrderByDescending(c => c.Params.Length).FirstOrDefault();
            if (ctor == null)
            {
                throw new Exception($"Type {typeInfo.ImplementingType} does not have a suitable constructor");
            }

            object implementingInstance = ctor.Ctor.Invoke(ResolveTypesToInstances(ctor.Params.Select(p => p.ParameterType), previouslyCreatedInstances, ctorArgumentTypeResolver).ToArray());
            previouslyCreatedInstances[typeInfo.ImplementingType] = implementingInstance;
            foreach (Type suitableInterface in typeInfo.AssignableInterfaces)
            {
                Type lookupType = suitableInterface.GenericTypeArguments[0];
                if (lookup.TryGetValue(lookupType, out TBaseInterface instance))
                {
                    string priorImplementor = previouslyCreatedInstances.Keys.Where(k => !k.IsInstanceOfType(typeInfo.ImplementingType)).FirstOrDefault(k => k.GetInterfaces().Any(i => i == suitableInterface)).GetNiceName();
                    string newImplementor = typeInfo.ImplementingType.GetNiceName();
                    throw new Exception($"Interface '{suitableInterface.GetNiceName()}' has multiple implementors, '{priorImplementor}' and '{newImplementor}'");
                }
                lookup[lookupType] = (TBaseInterface)Activator.CreateInstance(wrapperGenericTypeDef.MakeGenericType(lookupType), implementingInstance);
            }
        }

        return new TypeLookup<TBaseInterface>(lookup);
    }

    public IEnumerator<KeyValuePair<Type, TBaseInterface>> GetEnumerator()
    {
        return lookup.GetEnumerator();
    }

    public bool ContainsKey(Type key)
    {
        return lookup.ContainsKey(key);
    }

    public bool TryGetValue(Type key, out TBaseInterface value)
    {
        return lookup.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
