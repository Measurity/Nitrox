#if NET9_0_OR_GREATER
using ByteArray = System.ReadOnlySpan<byte>;
#else
using ByteArray = byte[];
#endif
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NitroxModel.Security;

public class AsymCrypto : IDisposable
{
    private const int KEY_SIZE = 4096;
    private RSA algorithm;
    public int KeySize { get; private init; }

    /// <summary>
    ///     Gets the public key data from the underlying asymmetric encryption algorithm.
    /// </summary>
    /// <remarks>
    ///     This data is safe to share with third-parties.
    /// </remarks>
    public byte[] PublicKey { get; private set; } = [];

    private AsymCrypto()
    {
    }

    public static AsymCrypto Load(byte[] publicKeyBytes, int keySize = KEY_SIZE)
    {
        RSA rsa = RSA.Create(keySize);
        rsa.ImportRSAPublicKey(publicKeyBytes, out int _);
        return new AsymCrypto
        {
            algorithm = rsa,
            PublicKey = publicKeyBytes,
            KeySize = keySize
        };
    }

    public static async Task<AsymCrypto> CreateOrLoad(Stream pemStream, int keySize = KEY_SIZE)
    {
        AsymCrypto result = new() { KeySize = keySize };
        RSA rsa = null;

        // Create new key if empty PEM stream.
        if (pemStream.Length == 0)
        {
            rsa = RSA.Create(keySize);
            await rsa.ExportAsPem(pemStream);
        }
        pemStream.Position = 0;
        string content;
        using (StreamReader reader = new(pemStream, Encoding.UTF8, false, 4096, true))
        {
            content = await reader.ReadToEndAsync();
        }

        // Try load by private key (which will infer public key), or public key only.
        rsa ??= RSA.Create(keySize);
        if (ExtractPemPart(content, "PRIVATE") is [..] privateBytes)
        {
            rsa.ImportRSAPrivateKey(privateBytes, out _);
            result.PublicKey = rsa.ExportRSAPublicKey();
        }
        else if (ExtractPemPart(content, "PUBLIC") is [..] publicBytes)
        {
            result.PublicKey = publicBytes;
            rsa.ImportRSAPublicKey(publicBytes, out _);
        }

        result.algorithm = rsa;
        return result;
    }

    /// <summary>
    ///     Encrypts the data using the public key.
    /// </summary>
    public byte[] Encrypt(ByteArray data) => algorithm.Encrypt(data, GetPadding());

    /// <summary>
    ///     Decrypts the data using the private key.
    /// </summary>
    public byte[] Decrypt(ByteArray data) => algorithm.Decrypt(data, GetPadding());

    public void Dispose() => algorithm?.Dispose();

    private static byte[] ExtractPemPart(string pemContent, string pemSegmentSearch)
    {
        Match match = Regex.Match(pemContent, @$"\-+(?:BEGIN[^-]+{pemSegmentSearch})[^\n]+\n([^\-]+)\n\-+(?:END[^-]+{pemSegmentSearch})[^\n]+");
        if (!match.Success)
        {
            return [];
        }
        return Convert.FromBase64String(match.Groups[1].Value.Replace("\r", "").Replace("\n", ""));
    }

    private RSAEncryptionPadding GetPadding() =>
        KeySize switch
        {
            <= 512 => RSAEncryptionPadding.OaepSHA1,
            < 1024 => RSAEncryptionPadding.OaepSHA256,
            _ => RSAEncryptionPadding.CreateOaep(new HashAlgorithmName("SHA3-512"))
        };
}
