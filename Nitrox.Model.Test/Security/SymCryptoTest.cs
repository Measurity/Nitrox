// using System.Text;
//
// namespace NitroxModel.Security;
//
// [TestClass]
// public class SymCryptoTest
// {
//     private readonly SymCrypto.Options testSymCryptoOptions = SymCrypto.Options.FastAndInsecure();
//
//     [TestMethod]
//     public void CanDecryptEncryptedDataWithSamePassword()
//     {
//         SymCrypto crypto = new("myK3y", testSymCryptoOptions);
//         byte[] data = "Hello, world!"u8.ToArray();
//         byte[] encrypted = crypto.Encrypt(data);
//         crypto.Decrypt(encrypted, out byte[] decrypted);
//         SymCrypto crypto2 = new("myK3y", testSymCryptoOptions);
//         crypto2.Decrypt(encrypted, out byte[] decrypted2);
//
//         Assert.AreNotEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(encrypted));
//         Assert.AreEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(decrypted));
//         Assert.AreEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(decrypted2));
//     }
//
//     [TestMethod]
//     public void ShouldFailDecryptWithDifferentPassword()
//     {
//         SymCrypto crypto = new("myK3y", testSymCryptoOptions);
//         byte[] data = "Hello, world!"u8.ToArray();
//         byte[] encrypted = crypto.Encrypt(data);
//         SymCrypto crypto2 = new("myKey", testSymCryptoOptions);
//         crypto2.Decrypt(encrypted, out byte[] decrypted);
//
//         Assert.AreNotEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(encrypted));
//         Assert.AreNotEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(decrypted));
//     }
// }
