#if NET9_0_OR_GREATER
using System;

namespace NitroxModel.Extensions;

public static class SpanExtensions
{
    public static bool EqualsAny(this ReadOnlySpan<char> input, StringComparison comparison = StringComparison.Ordinal, params ReadOnlySpan<string> compares)
    {
        foreach (string compare in compares)
        {
            if (input.Equals(compare, comparison))
            {
                return true;
            }
        }
        return false;
    }
}
#endif
