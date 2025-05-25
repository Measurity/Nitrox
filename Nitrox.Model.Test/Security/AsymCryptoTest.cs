using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NitroxModel.Security;

[TestClass]
public class AsymCryptoTest
{
    [TestMethod]
    public async Task CanDecryptEncryptedData()
    {
        AsymCrypto crypto = await CreateCrypto();
        byte[] data = "Hello, world!"u8.ToArray();
        byte[] encrypted = crypto.Encrypt(data);
        byte[] decrypted = crypto.Decrypt(encrypted);

        Assert.AreNotEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(encrypted));
        Assert.AreEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(decrypted));
    }

    [TestMethod]
    public async Task CanEncryptWithOnlyPublicKey()
    {
        AsymCrypto temp = await CreateCrypto();
        AsymCrypto crypto = AsymCrypto.Load(temp.PublicKey, temp.KeySize);
        byte[] data = "Hello, world!"u8.ToArray();
        byte[] encrypted = crypto.Encrypt(data);
        byte[] decrypted = temp.Decrypt(encrypted);

        Assert.AreNotEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(encrypted));
        Assert.Throws<CryptographicException>(() => crypto.Decrypt(encrypted));
        Assert.AreEqual(Encoding.UTF8.GetString(data), Encoding.UTF8.GetString(decrypted));
    }

    private static async Task<AsymCrypto> CreateCrypto()
    {
        using MemoryStream ms = new();
        return await AsymCrypto.CreateOrLoad(ms, 512);
    }
}
