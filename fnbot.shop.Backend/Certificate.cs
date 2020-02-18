using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fnbot.shop.Web;

namespace fnbot.shop.Backend
{
    public class Certificate
    {
        const uint MAGIC = 0xae789b23;
        const bool CHECK_REVOCATION = false;

        bool isDirty;
        string _name;
        DateTimeOffset _expiration;
        byte[] _publickey;

        public string Name
        {
            get => _name;
            set
            {
                isDirty = true;
                _name = value;
            }
        }
        public DateTimeOffset Expiration
        {
            get => _expiration;
            set
            {
                isDirty = true;
                _expiration = value;
            }
        }
        public byte[] PublicKey
        {
            get => _publickey;
            set
            {
                isDirty = true;
                _publickey = value;
            }
        }
        byte[] CertHash;
        byte[] RevocHash;

        public Certificate(string name, DateTimeOffset expiration, byte[] key)
        {
            Name = name;
            if (expiration < DateTimeOffset.UtcNow)
            {
                throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration is in the past");
            }
            Expiration = expiration;
            PublicKey = key;
        }

        public Certificate(Stream stream)
        {
            using (var hash = new SHA512CryptoServiceProvider())
            using (var cryptoStream = new CryptoStream(stream, hash, CryptoStreamMode.Read, true))
            using (var reader = new BinaryReader(cryptoStream, Encoding.Default, true))
            {
                if (reader.ReadUInt32() != MAGIC)
                {
                    throw new FileLoadException("Invalid file magic");
                }
                var fileVersion = reader.ReadUInt16();
                if (fileVersion != 0)
                {
                    throw new FileLoadException($"Cannot load future file version {fileVersion}");
                }
                Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                Expiration = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                PublicKey = reader.ReadBytes(reader.ReadUInt16());

                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                RevocHash = hash.Hash;

                CertHash = reader.ReadBytes(reader.ReadUInt16());
            }
            isDirty = false;
        }

        public void UpdateHash(byte[] parentPrivateKey)
        {
            if (isDirty)
            {
                using (var hash = new SHA512CryptoServiceProvider())
                using (var memStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memStream, hash, CryptoStreamMode.Write, true))
                using (var writer = new BinaryWriter(cryptoStream, Encoding.Default, true))
                {
                    writer.Write(MAGIC);
                    writer.Write((ushort)0); // file version
                    {
                        writer.Write((byte)Name.Length);
                        writer.Write(Encoding.UTF8.GetBytes(Name));
                    }
                    writer.Write(Expiration.ToUnixTimeMilliseconds());
                    {
                        writer.Write((ushort)PublicKey.Length);
                        writer.Write(PublicKey);
                    }

                    cryptoStream.FlushFinalBlock();
                    RevocHash = hash.Hash;
                }
                using var ecdsa = new ECDsaCng(CngKey.Import(parentPrivateKey, CngKeyBlobFormat.EccPrivateBlob));
                CertHash = ecdsa.SignHash(RevocHash);
                isDirty = false;
            }
        }

        public async Task<VerifyError> VerifyAsync(Certificate parentCert, Client client)
        {
            if (isDirty)
            {
                throw new InvalidOperationException("Update certificate hashes before verifying");
            }
            if (Expiration < DateTimeOffset.UtcNow)
            {
                return VerifyError.EXPIRED;
            }
            if (CHECK_REVOCATION)
            {
                var revocHash = ToHex(RevocHash);
                foreach (var line in (await client.GetAsync("https://fnbot.shop/api/revoc")).Split('\n'))
                {
                    if (line == revocHash)
                        return VerifyError.REVOKED;
                }
            }
            using var ecdsa = new ECDsaCng(CngKey.Import(parentCert.PublicKey, CngKeyBlobFormat.EccPublicBlob));

            return ecdsa.VerifyHash(RevocHash, CertHash) ?
                VerifyError.SUCCESS :
                VerifyError.INVALID;
        }

        public void Export(Stream stream)
        {
            if (isDirty)
            {
                throw new InvalidOperationException("Update certificate hashes before exporting");
            }
            using (var writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                writer.Write(MAGIC);
                writer.Write((ushort)0); // file version
                {
                    writer.Write((byte)Name.Length);
                    writer.Write(Encoding.UTF8.GetBytes(Name));
                }
                writer.Write(Expiration.ToUnixTimeMilliseconds());
                {
                    writer.Write((ushort)PublicKey.Length);
                    writer.Write(PublicKey);
                }
                {
                    writer.Write((ushort)CertHash.Length);
                    writer.Write(CertHash);
                }
            }
        }

        public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeys()
        {
            using var ecdsa = new ECDsaCng(521);
            return (ecdsa.Key.Export(CngKeyBlobFormat.EccPublicBlob), ecdsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob));
        }

        public enum VerifyError
        {
            SUCCESS,
            INVALID,
            EXPIRED,
            REVOKED
        }

        static Certificate()
        {
            _Lookup32 = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                var s = i.ToString("x2");
                _Lookup32[i] = s[0] + ((uint)s[1] << 16);
            }
        }
        static readonly uint[] _Lookup32;
        internal static string ToHex(ReadOnlySpan<byte> bytes)
        {
            var result = new char[128];
            for (int i = 0; i < 64; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }
    }
}
