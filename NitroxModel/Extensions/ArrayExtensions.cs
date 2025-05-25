using System;

namespace NitroxModel.Extensions;

public static class ArrayExtensions
{
    public static int GetIndex<T>(this T[] list, T itemToFind) => Array.IndexOf(list, itemToFind);
}
