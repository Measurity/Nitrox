using System.Reflection;

namespace TestHelper;

public static class Extensions
{
    public static bool IsTestAssembly(this Assembly assembly)
    {
        string name = assembly.GetName().Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        if (name.EndsWith(".Test") || name.EndsWith(".Tests"))
        {
            return true;
        }
        if (name.Contains(".Test.") || name.Contains(".Tests."))
        {
            return true;
        }
        return false;
    }

    public static Type GetMemberType(this MemberInfo memberInfo) => memberInfo switch
    {
        PropertyInfo propertyInfo => propertyInfo.PropertyType,
        FieldInfo fieldInfo => fieldInfo.FieldType,
        _ => throw new ArgumentException($"Invalid member of type {memberInfo.GetType()}")
    };

    public static bool HasBinaryPackIgnoreAttribute(this MemberInfo member) => member.GetCustomAttributes().All(a => a.GetType().Name != "IgnoredMemberAttribute");

    public static object InvokeInstanceMethod(this object obj, string name, params object[] args) => obj.GetType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance, null, obj, args);

    public static bool IsCollection(this Type t, out CollectionType collectionType)
    {
        if (t.IsArray && t.GetArrayRank() == 1)
        {
            collectionType = CollectionType.ARRAY;
            return true;
        }

        if (t.IsGenericType)
        {
            Type[] genericInterfacesDefinition = t.GetInterfaces()
                                                  .Where(i => i.IsGenericType)
                                                  .Select(i => i.GetGenericTypeDefinition())
                                                  .ToArray();

            if (genericInterfacesDefinition.Any(i => i == typeof(IList<>)))
            {
                collectionType = CollectionType.LIST;
                return true;
            }

            if (genericInterfacesDefinition.Any(i => i == typeof(IDictionary<,>)))
            {
                collectionType = CollectionType.DICTIONARY;
                return true;
            }

            if (genericInterfacesDefinition.Any(i => i == typeof(ISet<>)))
            {
                collectionType = CollectionType.SET;
                return true;
            }

            Type genericTypeDefinition = t.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(Queue<>) || genericTypeDefinition.Name.IndexOf("Queue", StringComparison.Ordinal) >= 0) // Queue has no defining interface
            {
                collectionType = CollectionType.QUEUE;
                return true;
            }
        }

        collectionType = CollectionType.NONE;
        return false;
    }

    public enum CollectionType
    {
        NONE,
        ARRAY,
        LIST,
        DICTIONARY,
        SET,
        QUEUE
    }
}
