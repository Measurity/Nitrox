// using System;
// using System.Buffers;
// using System.Security.Cryptography;
//
// namespace NitroxModel.Security;
//
// public class SymCrypto : IDisposable
// {
//     /// <summary>
//     ///     Salt used when deriving an AES key from a password.
//     ///     Do NOT change as it will break decryption of previously encrypted data!
//     /// </summary>
//     private static readonly byte[] keySalt = [225, 112, 13, 144, 132, 11, 223, 217, 248, 102, 126, 33, 126, 153, 130, 66];
//
//     private readonly Aes aes;
//     private readonly byte[] ivTempBytes;
//     private readonly LockObject locker = new();
//     private readonly ArrayPool<byte> pool;
//
//     public SymCrypto(string password, Options options = default)
//     {
//         options ??= new Options();
//
//         aes = Aes.Create();
//         aes.KeySize = options.KeySize;
//         pool = ArrayPool<byte>.Create();
//         ivTempBytes = new byte[aes.BlockSize / 8];
//
//         Span<byte> derivedKey = new byte[aes.KeySize / 8];
//         // See https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html#pbkdf2 for recommended [iterations + hash algorithm] combo.
//         Rfc2898DeriveBytes.Pbkdf2(password, keySalt, derivedKey, options.DerivingKeyIterations, HashAlgorithmName.SHA3_256);
//         aes.Key = [.. derivedKey];
//     }
//
//     public byte[] Encrypt(byte[] data)
//     {
//         lock (locker)
//         {
//             ivTempBytes.CryptoRngFill();
//             int dataWithIvLength = data.Length + ivTempBytes.Length;
//             byte[] tempBytes = pool.Rent(dataWithIvLength);
//             try
//             {
//                 data.CopyTo(tempBytes.AsSpan());
//                 data.CopyTo(tempBytes.AsSpan().Slice(ivTempBytes.Length));
//                 return aes.EncryptCbc(tempBytes.AsSpan().Slice(0, dataWithIvLength), ivTempBytes);
//             }
//             finally
//             {
//                 pool.Return(tempBytes);
//             }
//         }
//     }
//
//     public bool Decrypt(ReadOnlySpan<byte> data, out byte[] decryptedData)
//     {
//         decryptedData = [];
//         lock (locker)
//         {
//             data.Slice(0, ivTempBytes.Length).CopyTo(ivTempBytes);
//             try
//             {
//                 decryptedData = aes.DecryptCbc(data.Slice(ivTempBytes.Length), ivTempBytes);
//                 return true;
//             }
//             catch (CryptographicException)
//             {
//                 return false;
//             }
//         }
//     }
//
//     public void Dispose()
//     {
//         aes?.Dispose();
//     }
//
//     public record Options(int KeySize = 256, int DerivingKeyIterations = 600_000)
//     {
//         public static Options FastAndInsecure() => new(128, 1_000);
//     }
// }
