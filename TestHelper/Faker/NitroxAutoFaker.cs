using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace TestHelper.Faker;

public class NitroxAutoFaker<T> : NitroxFaker, INitroxFaker
{
    private readonly ConstructorInfo constructor;
    private readonly MemberInfo[] memberInfos;
    private readonly INitroxFaker[] parameterFakers;

    public NitroxAutoFaker()
    {
        Type type = typeof(T);
        if (!IsValidType(type))
        {
            throw new InvalidOperationException($"{type.Name} is not a valid type for {nameof(NitroxAutoFaker<T>)}");
        }

        OutputType = type;
        FakerByType.Add(type, this);

        if (type.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0)
        {
            memberInfos = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                              .Where(member => member.GetCustomAttributes<DataMemberAttribute>().Any()).ToArray();
        }
        else
        {
            memberInfos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                              .Where(member => member.MemberType is MemberTypes.Field or MemberTypes.Property && !member.HasBinaryPackIgnoreAttribute())
                              .ToArray();
        }

        if (!TryGetConstructorForType(type, memberInfos, out constructor) &&
            !TryGetConstructorForType(type, [], out constructor))
        {
            throw new NullReferenceException($"Could not find a constructor with no parameters for {type}");
        }

        parameterFakers = new INitroxFaker[memberInfos.Length];
        Type[] constructorArgumentTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
        for (int i = 0; i < memberInfos.Length; i++)
        {
            Type dataMemberType = constructorArgumentTypes.Length == memberInfos.Length ? constructorArgumentTypes[i] : memberInfos[i].GetMemberType();

            if (FakerByType.TryGetValue(dataMemberType, out INitroxFaker memberFaker))
            {
                parameterFakers[i] = memberFaker;
            }
            else
            {
                parameterFakers[i] = CreateFaker(dataMemberType);
            }
        }
    }

    public INitroxFaker[] GetSubFakers() => parameterFakers;

    public T Generate()
    {
        ValidateFakerTree();
        return (T)GenerateUnsafe(new HashSet<Type>());
    }

    public object GenerateUnsafe(HashSet<Type> typeTree)
    {
        object[] parameterValues = new object[parameterFakers.Length];

        for (int i = 0; i < parameterValues.Length; i++)
        {
            INitroxFaker parameterFaker = parameterFakers[i];

            if (!typeTree.Add(parameterFaker.OutputType))
            {
                if (parameterFaker is NitroxCollectionFaker collectionFaker)
                {
                    parameterValues[i] = Activator.CreateInstance(collectionFaker.OutputCollectionType);
                }
                else
                {
                    parameterValues[i] = null;
                }
            }
            else
            {
                parameterValues[i] = parameterFakers[i].GenerateUnsafe(typeTree);
                typeTree.Remove(parameterFaker.OutputType);
            }
        }

        if (constructor.GetParameters().Length == parameterValues.Length)
        {
            try
            {
                return (T)constructor.Invoke(parameterValues);
            }
            catch (Exception ex)
            {
                throw new Exception($"Constructor call {constructor.DeclaringType}({string.Join(", ", constructor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}) failed", ex);
            }
        }

        T obj = (T)constructor.Invoke([]);
        for (int index = 0; index < memberInfos.Length; index++)
        {
            MemberInfo memberInfo = memberInfos[index];
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)memberInfo).SetValue(obj, parameterValues[index]);
                    break;
                case MemberTypes.Property:
                    PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                    if (!propertyInfo.CanWrite && parameterValues[index].GetType().IsCollection(out CollectionType collectionType))
                    {
                        object origColl = propertyInfo.GetValue(obj);

                        switch (collectionType)
                        {
                            case CollectionType.ARRAY:
                                for (int i = 0; i < ((Array)parameterValues[index]).Length; i++)
                                {
                                    // TODO: FIX!
                                    // origColl[i] = ((Array)parameterValues[index]).GetValue(i);
                                }
                                break;
                            case CollectionType.LIST:
                            case CollectionType.DICTIONARY:
                            case CollectionType.SET:
                                foreach (object createdValue in (IEnumerable)parameterValues[index])
                                {
                                    origColl.InvokeInstanceMethod("Add", createdValue);
                                }

                                break;
                            case CollectionType.QUEUE:
                                foreach (object createdValue in (IEnumerable)parameterValues[index])
                                {
                                    origColl.InvokeInstanceMethod("Enqueue", createdValue);
                                }

                                break;
                            case CollectionType.NONE:
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (propertyInfo.CanWrite)
                    {
                        propertyInfo.SetValue(obj, parameterValues[index]);
                    }
                    else
                    {
                        Regex backingFieldNameRegex = new($"\\A<{propertyInfo.Name}>k__BackingField\\Z");
                        FieldInfo backingField = propertyInfo.DeclaringType.GetRuntimeFields().FirstOrDefault(a => backingFieldNameRegex.IsMatch(a.Name));

                        if (backingField == null)
                        {
                            throw new InvalidOperationException($"{propertyInfo.DeclaringType}.{propertyInfo.Name} is not accessible for writing");
                        }

                        backingField.SetValue(obj, parameterValues[index]);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return obj;
    }

    private static bool TryGetConstructorForType(Type type, MemberInfo[] dataMembers, out ConstructorInfo constructorInfo)
    {
        foreach (ConstructorInfo constructor in type.GetConstructors())
        {
            if (constructor.GetParameters().Length != dataMembers.Length)
            {
                continue;
            }

            bool parameterValid = constructor.GetParameters()
                                             .All(parameter => dataMembers.Any(d => d.GetMemberType() == parameter.ParameterType &&
                                                                                    d.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)));

            if (parameterValid)
            {
                constructorInfo = constructor;
                return true;
            }
        }

        constructorInfo = null;
        return false;
    }

    private void ValidateFakerTree()
    {
        List<INitroxFaker> fakerTree = new();

        void ValidateFaker(INitroxFaker nitroxFaker)
        {
            if (fakerTree.Contains(nitroxFaker))
            {
                return;
            }

            fakerTree.Add(nitroxFaker);

            if (nitroxFaker is NitroxAbstractFaker abstractFaker)
            {
                NitroxCollectionFaker collectionFaker = (NitroxCollectionFaker)fakerTree.LastOrDefault(f => f.GetType() == typeof(NitroxCollectionFaker));
                if (collectionFaker != null)
                {
                    collectionFaker.GenerateSize = Math.Max(collectionFaker.GenerateSize, abstractFaker.AssignableTypesCount);
                }
            }

            foreach (INitroxFaker subFaker in nitroxFaker.GetSubFakers())
            {
                ValidateFaker(subFaker);
            }

            fakerTree.Remove(nitroxFaker);
        }

        foreach (INitroxFaker parameterFaker in parameterFakers)
        {
            ValidateFaker(parameterFaker);
        }
    }
}
