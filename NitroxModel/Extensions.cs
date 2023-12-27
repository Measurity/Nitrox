using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NitroxModel;

public static class Extensions
{
    public static TAttribute GetAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
    {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);

        return type.GetField(name)
                   .GetCustomAttributes(false)
                   .OfType<TAttribute>()
                   .SingleOrDefault();
    }

    /// <summary>
    ///     Removes all items from the list when the predicate returns true.
    /// </summary>
    /// <param name="list">The list to remove items from.</param>
    /// <param name="extraParameter">An extra parameter to supply to the predicate.</param>
    /// <param name="predicate">The predicate that tests each item in the list for removal.</param>
    public static void RemoveAllFast<TItem, TParameter>(this IList<TItem> list, TParameter extraParameter, Func<TItem, TParameter, bool> predicate)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            TItem item = list[i];
            if (predicate.Invoke(item, extraParameter))
            {
                // Optimization for Unity mono: swap item to end and remove it. This reduces GC pressure for resizing arrays.
                list[i] = list[^1];
                list.RemoveAt(list.Count - 1);
            }
        }
    }

    public static int GetIndex<T>(this T[] list, T itemToFind) => Array.IndexOf(list, itemToFind);

    public static string AsByteUnitText(this uint byteSize)
    {
        // Uint can't go past 4GiB, so we don't need to worry about overflow.
        string[] suf = { "B", "KiB", "MiB", "GiB" };
        if (byteSize == 0)
        {
            return $"0{suf[0]}";
        }

        int place = Convert.ToInt32(Math.Floor(Math.Log(byteSize, 1024)));
        double num = Math.Round(byteSize / Math.Pow(1024, place), 1);
        return num + suf[place];
    }

    public static string GetFirstNonAggregateMessage(this Exception exception) => exception switch
    {
        AggregateException ex => ex.InnerExceptions.FirstOrDefault(e => e is not AggregateException)?.Message ?? ex.Message,
        _ => exception.Message
    };

    /// <returns>
    ///     <inheritdoc cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})" /><br />
    ///     <see langword="true" /> if both IEnumerables are null.
    /// </returns>
    /// <remarks>
    ///     <see cref="ArgumentNullException" /> can't be thrown because of <paramref name="first" /> or
    ///     <paramref name="second" /> being null.
    /// </remarks>
    /// <inheritdoc cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})" />
    public static bool SequenceEqualOrBothNull<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        if (first != null && second != null)
        {
            return first.SequenceEqual(second);
        }

        return first == second;
    }

    public static void RemoveWhere<TKey, TValue, TParameter>(this IDictionary<TKey, TValue> dictionary, TParameter extraParameter, Func<TValue, TParameter, bool> predicate)
    {
        int toRemoveIndex = 0;
        TKey[] toRemove = ArrayPool<TKey>.Shared.Rent(dictionary.Count);
        try
        {
            foreach (KeyValuePair<TKey, TValue> item in dictionary)
            {
                if (predicate.Invoke(item.Value, extraParameter))
                {
                    toRemove[toRemoveIndex++] = item.Key;
                }
            }

            for (int i = 0; i < toRemoveIndex; i++)
            {
                dictionary.Remove(toRemove[i]);
            }
        }
        finally
        {
            ArrayPool<TKey>.Shared.Return(toRemove, true);
        }
    }

    public static string GetNiceName(this Type type)
    {
        static string CleanTypeName(string typeName)
        {
            int tickIndex = typeName.IndexOf('`');
            if (tickIndex >= 0)
            {
                typeName = typeName.Substring(0, tickIndex);
            }
            return typeName;
        }

        return type switch
        {
            { IsInterface: true, IsGenericType: true } => $"{CleanTypeName(type.Name)}<{string.Join(", ", type.GenericTypeArguments.Select(t => CleanTypeName(t.Name)))}>",
            _ => type.Name
        };
    }

    public static Dictionary<Type, TBaseInterface> CreateSingletonMappingForImplementingGenericInterface<TBaseInterface, TWrapper>(this IEnumerable<Type> types, Func<Type, object> ctorArgumentTypeResolver = null) where TWrapper : TBaseInterface
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

        // TODO: Turn this API into source generator.
        Type[] wrapperInterfaces = typeof(TWrapper).GetInterfaces();
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

        return lookup;
    }
}
