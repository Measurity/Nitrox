using System.Security.Cryptography;

namespace NitroxModel.Extensions;

public static class ByteArrayExtensions
{
    public static void CryptoRngFill(this byte[] data)
    {
        if (data is null or [])
        {
            return;
        }

#if NET9_0_OR_GREATER
        RandomNumberGenerator.Fill(data);
#else
        lock (rngLocker)
        {
            rng ??= RandomNumberGenerator.Create();
            rng.GetBytes(data);
        }
#endif
    }

#if !NET9_0_OR_GREATER
    private static RandomNumberGenerator rng;
    private static readonly object rngLocker = new();
#endif
}
