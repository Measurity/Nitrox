using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NitroxModel.Extensions;

/// <summary>
///     Extensions to help old .NET framework libraries to use PEM format for RSA.
/// </summary>
/// <remarks>
///     Code from <a href="https://github.com/jrnker/CSharp-easy-RSA-PEM">https://github.com/jrnker/CSharp-easy-RSA-PEM</a>
/// </remarks>
public static class RsaExtensions
{
    /// <summary>
    ///     Exports the RSA data into a PEM-format with PKCS1 encoded segments.
    /// </summary>
    public static async Task ExportAsPem(this RSA rsa, Stream pemStream)
    {
#if NET9_0_OR_GREATER
        await using StreamWriter stream = new(pemStream, Encoding.UTF8, leaveOpen: true);
#else
        using StreamWriter stream = new(pemStream, Encoding.UTF8, 4096, true);
#endif
        await stream.WriteLineAsync(rsa.ExportRSAPublicKeyPem());
        await stream.WriteLineAsync();
        await stream.WriteLineAsync(rsa.ExportRSAPrivateKeyPem());
    }

    private static RSAParameters GetRsaParametersFromPublicKey(byte[] publicKey)
    {
        using MemoryStream ms = new(publicKey);
        using BinaryReader reader = new(ms);
        try
        {
            ushort shortValue = reader.ReadUInt16();
            switch (shortValue)
            {
                case 0x8130:
                    // If true, data is little endian since the proper logical seq is 0x30 0x81
                    reader.ReadByte(); //advance 1 byte
                    break;
                case 0x8230:
                    reader.ReadInt16(); //advance 2 bytes
                    break;
                default:
                    throw new Exception("Improper ASN.1 format");
            }

            shortValue = reader.ReadUInt16();
            if (shortValue != 0x0102) // (version number)
            {
                throw new Exception("Improper ASN.1 format, unexpected version number");
            }

            byte byteValue = reader.ReadByte();
            if (byteValue != 0x00)
            {
                throw new Exception("Improper ASN.1 format");
            }

            // The data following the version will be the ASN.1 data itself, which in our case
            // are a sequence of integers.
            RSAParameters rsaParams = new();

            rsaParams.Modulus = reader.ReadBytes(GetIntegerSize(reader));

            // Argh, this is a pain. From empirical testing it appears to be that RSAParameters doesn't like byte buffers that
            // have their leading zeros removed.  The RFC doesn't address this area that I can see, so it's hard to say that this
            // is a bug, but it sure would be helpful if it allowed that. So, there's some extra code here that knows what the
            // sizes of the various components are supposed to be. Using these sizes we can ensure the buffer sizes are exactly
            // what the RSAParameters expect.
            RsaParameterTraits traits = new(rsaParams.Modulus.Length * 8);

            rsaParams.Modulus = AlignBytes(rsaParams.Modulus, traits.SizeMod);
            rsaParams.Exponent = AlignBytes(reader.ReadBytes(GetIntegerSize(reader)), traits.SizeExp);
            //rsAparams.D = Helpers.AlignBytes(rd.ReadBytes(GetIntegerSize(rd)), traits.size_D);
            //rsAparams.P = Helpers.AlignBytes(rd.ReadBytes(GetIntegerSize(rd)), traits.size_P);
            //rsAparams.Q = Helpers.AlignBytes(rd.ReadBytes(GetIntegerSize(rd)), traits.size_Q);
            //rsAparams.DP = Helpers.AlignBytes(rd.ReadBytes(GetIntegerSize(rd)), traits.size_DP);
            //rsAparams.DQ = Helpers.AlignBytes(rd.ReadBytes(GetIntegerSize(rd)), traits.size_DQ);
            //rsAparams.InverseQ = Helpers.AlignBytes(rd.ReadBytes(GetIntegerSize(rd)), traits.size_InvQ);

            return rsaParams;
        }
        finally
        {
            reader.Close();
        }
    }

    private static RSAParameters GetRsaParametersFromPrivateKey(byte[] privateKey)
    {
        RSAParameters rsaParams = new();
        using (BinaryReader reader = new(new MemoryStream(privateKey)))
        {
            ushort twobytes = reader.ReadUInt16();
            if (twobytes == 0x8130)
            {
                reader.ReadByte();
            }
            else if (twobytes == 0x8230)
            {
                reader.ReadInt16();
            }
            else
            {
                throw new Exception("Unexpected value read");
            }
            twobytes = reader.ReadUInt16();
            if (twobytes != 0x0102)
            {
                throw new Exception("Unexpected version");
            }
            byte bt = reader.ReadByte();
            if (bt != 0x00)
            {
                throw new Exception("Unexpected value read");
            }

            rsaParams.Modulus = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.Exponent = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.D = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.P = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.Q = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.DP = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.DQ = reader.ReadBytes(GetIntegerSize(reader));
            rsaParams.InverseQ = reader.ReadBytes(GetIntegerSize(reader));
        }
        return rsaParams;
    }

    private static int GetIntegerSize(BinaryReader reader)
    {
        int count;
        byte bt = reader.ReadByte();
        if (bt != 0x02)
        {
            return 0;
        }
        bt = reader.ReadByte();

        if (bt == 0x81)
        {
            count = reader.ReadByte();
        }
        else if (bt == 0x82)
        {
            byte highByte = reader.ReadByte();
            byte lowByte = reader.ReadByte();
            byte[] modInt = [lowByte, highByte, 0x00, 0x00];
            count = BitConverter.ToInt32(modInt, 0);
        }
        else
        {
            count = bt;
        }

        while (reader.ReadByte() == 0x00)
        {
            count -= 1;
        }
        reader.BaseStream.Seek(-1, SeekOrigin.Current);
        return count;
    }

    private static byte[] AlignBytes(byte[] inputBytes, int alignSize)
    {
        int inputBytesSize = inputBytes.Length;

        if (alignSize != -1 && inputBytesSize < alignSize)
        {
            byte[] buf = new byte[alignSize];
            for (int i = 0; i < inputBytesSize; ++i)
            {
                buf[i + (alignSize - inputBytesSize)] = inputBytes[i];
            }
            return buf;
        }
        return inputBytes; // Already aligned, or doesn't need alignment
    }

    private static void EncodeLength(BinaryWriter stream, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative");
        }
        if (length < 0x80)
        {
            // Short form
            stream.Write((byte)length);
        }
        else
        {
            // Long form
            int temp = length;
            int bytesRequired = 0;
            while (temp > 0)
            {
                temp >>= 8;
                bytesRequired++;
            }
            stream.Write((byte)(bytesRequired | 0x80));
            for (int i = bytesRequired - 1; i >= 0; i--)
            {
                stream.Write((byte)((length >> (8 * i)) & 0xff));
            }
        }
    }

    private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
    {
        stream.Write((byte)0x02); // INTEGER
        int prefixZeros = 0;
        foreach (byte b in value)
        {
            if (b != 0)
            {
                break;
            }
            prefixZeros++;
        }
        if (value.Length - prefixZeros == 0)
        {
            EncodeLength(stream, 1);
            stream.Write((byte)0);
        }
        else
        {
            if (forceUnsigned && value[prefixZeros] > 0x7f)
            {
                // Add a prefix zero to force unsigned if the MSB is 1
                EncodeLength(stream, value.Length - prefixZeros + 1);
                stream.Write((byte)0);
            }
            else
            {
                EncodeLength(stream, value.Length - prefixZeros);
            }
            for (int i = prefixZeros; i < value.Length; i++)
            {
                stream.Write(value[i]);
            }
        }
    }

    private class RsaParameterTraits
    {
        public readonly int SizeD;
        public readonly int SizeDp;
        public readonly int SizeDq;
        public readonly int SizeExp;
        public readonly int SizeInvQ;
        public readonly int SizeMod;
        public readonly int SizeP;
        public readonly int SizeQ;

        public RsaParameterTraits(int modulusLengthInBits)
        {
            // The modulus length is supposed to be one of the common lengths, which is the commonly referred to strength of the key,
            // like 1024 bit, 2048 bit, etc.  It might be a few bits off though, since if the modulus has leading zeros it could show
            // up as 1016 bits or something like that.
            int assumedLength = -1;
            double logBase = Math.Log(modulusLengthInBits, 2);
            if (logBase == (int)logBase)
            {
                // It's already an even power of 2
                assumedLength = modulusLengthInBits;
            }
            else
            {
                // It's not an even power of 2, so round it up to the nearest power of 2.
                assumedLength = (int)(logBase + 1.0);
                assumedLength = (int)Math.Pow(2, assumedLength);
                Debug.Assert(false); // Can this really happen in the field?  I've never seen it, so if it happens
                // you should verify that this really does the 'right' thing!
            }

            SizeMod = assumedLength / 8;
            SizeExp = -1;
            SizeD = assumedLength / 8;
            SizeP = assumedLength / 16;
            SizeQ = assumedLength / 16;
            SizeDp = assumedLength / 16;
            SizeDq = assumedLength / 16;
            SizeInvQ = assumedLength / 16;
        }
    }
#if !NET9_0_OR_GREATER
    // ReSharper disable once InconsistentNaming
    public static void ImportRSAPublicKey(this RSA rsa, byte[] publicKey, out int bytesRead)
    {
        bytesRead = publicKey.Length;
        rsa.ImportParameters(GetRsaParametersFromPublicKey(publicKey));
    }

    // ReSharper disable once InconsistentNaming
    public static void ImportRSAPrivateKey(this RSA rsa, byte[] privateKey, out int bytesRead)
    {
        bytesRead = privateKey.Length;
        rsa.ImportParameters(GetRsaParametersFromPrivateKey(privateKey));
    }

    /// <summary>
    ///     Gets the private key as a PKCS1 encoded PEM-file segment.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static string ExportRSAPrivateKeyPem(this RSA rsa) // PKCS1
    {
        RSAParameters parameters = rsa.ExportParameters(true);
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write((byte)0x30); // SEQUENCE
        using (MemoryStream innerStream = new())
        {
            using BinaryWriter innerWriter = new(innerStream);
            EncodeIntegerBigEndian(innerWriter, [0x00]); // Version
            EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
            EncodeIntegerBigEndian(innerWriter, parameters.D);
            EncodeIntegerBigEndian(innerWriter, parameters.P);
            EncodeIntegerBigEndian(innerWriter, parameters.Q);
            EncodeIntegerBigEndian(innerWriter, parameters.DP);
            EncodeIntegerBigEndian(innerWriter, parameters.DQ);
            EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
            int length = (int)innerStream.Length;
            EncodeLength(writer, length);
            writer.Write(innerStream.ToArray(), 0, length);
        }

        return PackagePem(stream.ToArray(), "RSA PRIVATE KEY");
    }

    // ReSharper disable once InconsistentNaming - match name of function in .NET Framework.
    public static byte[] ExportRSAPublicKey(this RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(false);
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write((byte)0x30); // SEQUENCE
        using (MemoryStream innerStream = new())
        {
            using BinaryWriter innerWriter = new(innerStream);
            EncodeIntegerBigEndian(innerWriter, [0x00]); // Version
            EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent);

            // All Parameter Must Have Value so Set Other Parameter Value Whit Invalid Data  (for keeping Key Structure  use "parameters.Exponent" value for invalid data)
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.D
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.P
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.Q
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DP
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DQ
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.InverseQ

            int length = (int)innerStream.Length;
            EncodeLength(writer, length);
            writer.Write(innerStream.ToArray(), 0, length);
        }
        return stream.ToArray();
    }

    /// <summary>
    ///     Gets the public key as a PKCS1 encoded PEM-file segment.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static string ExportRSAPublicKeyPem(this RSA rsa) // PKCS1
        => PackagePem(rsa.ExportRSAPublicKey(), "RSA PUBLIC KEY");

    private static string PackagePem(byte[] bytes, string header)
    {
        using TextWriter outputStream = new StringWriter();
        outputStream.NewLine = "\n";

        char[] base64 = Convert.ToBase64String(bytes, 0, bytes.Length).ToCharArray();
        outputStream.Write("-----BEGIN ");
        outputStream.Write(header);
        outputStream.WriteLine("-----");

        // Output as Base64 with lines chopped at 64 characters
        for (int i = 0; i < base64.Length; i += 64)
        {
            outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
        }
        outputStream.Write("-----END ");
        outputStream.Write(header);
        outputStream.WriteLine("-----");
        return outputStream.ToString();
    }
#endif
}
