using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NitroxModel.Extensions;

public static class StringExtensions
{
    public static string ReplaceInvalidFileNameCharacters(this string fileName)
    {
        foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidFileNameChar, ' ');
        }
        return fileName.Trim();
    }

    public static byte[] AsMd5Hash(this string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        return md5.ComputeHash(inputBytes);
    }
}
