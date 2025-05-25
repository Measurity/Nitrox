using System.Collections;
using System.Reflection;

namespace TestHelper.Faker;

public class NitroxCollectionFaker : NitroxFaker, INitroxFaker
{
    private const int DEFAULT_SIZE = 2;

    public static bool TryGetCollectionTypes(Type type, out Type[] types)
    {
        if (!type.IsCollection(out CollectionType collectionType))
        {
            types = [];
            return false;
        }

        if (collectionType == CollectionType.ARRAY)
        {
            types = [type.GetElementType()];
            return true;
        }

        types = type.GenericTypeArguments;
        return true;
    }

    public int GenerateSize = DEFAULT_SIZE;
    public Type OutputCollectionType;

    private readonly INitroxFaker[] subFakers;
    private readonly Func<HashSet<Type>, object> generateAction;

    public NitroxCollectionFaker(Type type, CollectionType collectionType)
    {
        OutputCollectionType = type;
        INitroxFaker elementFaker;

        switch (collectionType)
        {
            case CollectionType.ARRAY:
                Type arrayType = OutputType = type.GetElementType();
                elementFaker = GetOrCreateFaker(arrayType);
                subFakers = [elementFaker];

                generateAction = typeTree =>
                {
                    typeTree.Add(arrayType);
                    Array array = Array.CreateInstance(arrayType, GenerateSize);

                    for (int i = 0; i < GenerateSize; i++)
                    {
                        array.SetValue(elementFaker.GenerateUnsafe(typeTree), i);
                    }

                    typeTree.Remove(arrayType);
                    return array;
                };
                break;
            case CollectionType.LIST:
            case CollectionType.SET:
                Type listType = OutputType = type.GenericTypeArguments[0];
                elementFaker = GetOrCreateFaker(listType);
                subFakers = [elementFaker];

                generateAction = typeTree =>
                {
                    typeTree.Add(listType);
                    object list = Activator.CreateInstance(type);

                    for (int i = 0; i < GenerateSize; i++)
                    {
                        MethodInfo castMethod = CastMethodBase.MakeGenericMethod(OutputType);
                        object castedObject = castMethod.Invoke(null, [elementFaker.GenerateUnsafe(typeTree)]);
                        list.InvokeInstanceMethod("Add", castedObject);
                    }

                    typeTree.Remove(listType);
                    return list;
                };
                break;
            case CollectionType.DICTIONARY:
                Type[] dicType = type.GenericTypeArguments;
                OutputType = dicType[1]; // A little hacky but should work as we don't use circular dependencies as keys
                INitroxFaker keyFaker = GetOrCreateFaker(dicType[0]);
                INitroxFaker valueFaker = GetOrCreateFaker(dicType[1]);
                subFakers = [keyFaker, valueFaker];

                generateAction = typeTree =>
                {
                    typeTree.Add(dicType[0]);
                    typeTree.Add(dicType[1]);
                    IDictionary dict = (IDictionary)Activator.CreateInstance(type);
                    for (int i = 0; i < GenerateSize; i++)
                    {
                        for (int tries = 0; tries < 10; tries++)
                        {
                            object key = keyFaker.GenerateUnsafe(typeTree);

                            if (!dict.Contains(key))
                            {
                                dict.Add(key, valueFaker.GenerateUnsafe(typeTree));
                                break;
                            }

                            if (tries == 9)
                            {
                                throw new InvalidOperationException($"While generating action for filling Dictionary an unique key of {dicType[0]} couldn't be generated even after 10 tries");
                            }
                        }
                    }

                    typeTree.Remove(dicType[0]);
                    typeTree.Remove(dicType[1]);
                    return dict;
                };
                break;
            case CollectionType.QUEUE:
                Type queueType = OutputType = type.GenericTypeArguments[0];
                elementFaker = GetOrCreateFaker(queueType);
                subFakers = [elementFaker];

                generateAction = typeTree =>
                {
                    typeTree.Add(queueType);
                    object queue = Activator.CreateInstance(type);

                    for (int i = 0; i < GenerateSize; i++)
                    {
                        MethodInfo castMethod = CastMethodBase.MakeGenericMethod(OutputType);
                        object castedObject = castMethod.Invoke(null, [elementFaker.GenerateUnsafe(typeTree)]);
                        queue.InvokeInstanceMethod("Enqueue", castedObject);
                    }

                    typeTree.Remove(queueType);
                    return queue;
                };
                break;
            case CollectionType.NONE:
            default:
                throw new ArgumentOutOfRangeException(nameof(collectionType), collectionType, null);
        }
    }

    public INitroxFaker[] GetSubFakers() => subFakers;

    public object GenerateUnsafe(HashSet<Type> typeTree) => generateAction.Invoke(typeTree);
}
