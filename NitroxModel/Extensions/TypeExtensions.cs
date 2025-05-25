using System;

namespace NitroxModel.Extensions;

public static class TypeExtensions
{
    public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
    {
        if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
        {
            return true;
        }

        Type givenBaseType = givenType.BaseType;
        if (givenBaseType == null)
        {
            return false;
        }

        return IsAssignableToGenericType(givenBaseType, genericType);
    }
}
