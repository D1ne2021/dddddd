using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fnbot.shop.Web;
using static fnbot.shop.Backend.Certificate;

namespace fnbot.shop.Backend
{
    public class Plugin
    {
        const uint MAGIC = 0xe926b4a6;
        const uint MAGIC_VERIFIED = 0xe621b5b6;

        bool isDirty;
        string _name;
        string _description;
        Type _plugintype;
        Guid _guid;
        string _version;
        string _main;
        Certificate _certificate;

        public string Name
        {
            get => _name;
            set
            {
                isDirty = true;
                _name = value;
            }
        }
        public string Description
        {
            get => _description;
            set
            {
                isDirty = true;
                _description = value;
            }
        }
        public Type PluginType
        {
            get => _plugintype;
            set
            {
                isDirty = true;
                _plugintype = value;
            }
        }
        public Guid GUID
        {
            get => _guid;
            set
            {
                isDirty = true;
                _guid = value;
            }
        }
        public string Version
        {
            get => _version;
            set
            {
                isDirty = true;
                _version = value;
            }
        }
        public string Main
        {
            get => _main;
            set
            {
                isDirty = true;
                _main = value;
            }
        }
        public Certificate Certificate
        {
            get => _certificate;
            set
            {
                isDirty = true;
                _certificate = value;
            }
        }
        byte[] PluginHash;
        byte[] RevocHash;

        public MemoryStream DLL { get; set; }
        public MemoryStream PDB { get; set; }
        public MemoryStream Icon { get; set; }

        public bool Verified => Certificate != null;

        public Plugin(string name, Type type, Guid guid)
        {
            Name = name;
            PluginType = type;
            GUID = guid;
        }

        public Plugin(Stream stream)
        {
            bool verified;
            {
                byte[] buf = new byte[sizeof(uint)];
                stream.Read(buf, 0, sizeof(uint));
                var magic = BitConverter.ToUInt32(buf);
                if (magic == MAGIC)
                {
                    verified = false;
                }
                else if (magic == MAGIC_VERIFIED)
                {
                    verified = true;
                }
                else
                {
                    throw new FileLoadException("Invalid file magic");
                }
            }
            SHA512CryptoServiceProvider hash = null;
            CryptoStream cryptoStream = null;
            if (verified)
            {
                hash = new SHA512CryptoServiceProvider();
                cryptoStream = new CryptoStream(stream, hash, CryptoStreamMode.Read, true);
            }
            using (hash)
            using (cryptoStream)
            using (var reader = new BinaryReader(cryptoStream ?? stream, Encoding.Default, true))
            {
                var fileVersion = reader.ReadUInt16();
                if (fileVersion != 0)
                {
                    throw new FileLoadException($"Cannot load future file version {fileVersion}");
                }
                PluginType = (Type)reader.ReadByte();
                GUID = new Guid(reader.ReadBytes(16));
                Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                Description = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                Version = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                Main = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                Icon = new MemoryStream(reader.ReadInt32());
                Icon.Write(reader.ReadBytes(Icon.Capacity));
                {
                    DLL = new MemoryStream(reader.ReadInt32());
                    DLL.Write(reader.ReadBytes(DLL.Capacity));
                }
                {
                    int len = reader.ReadInt32();
                    if (len != 0)
                    {
                        PDB = new MemoryStream(len);
                        PDB.Write(reader.ReadBytes(PDB.Capacity));
                    }
                }
                if (verified)
                {
                    Certificate = new Certificate(cryptoStream);
                    hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    RevocHash = hash.Hash;
                }
            }
            if (verified)
            {
                using var reader = new BinaryReader(stream, Encoding.Default, true);
                PluginHash = reader.ReadBytes(reader.ReadUInt16());
            }
            isDirty = false;
        }

        public void UpdateHash(byte[] certificatePrivateKey)
        {
            if (!Verified)
            {
                throw new InvalidOperationException("Cannot update hashes for a non-verified plugin");
            }
            if (isDirty)
            {
                using (var hash = new SHA512CryptoServiceProvider())
                using (var memStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memStream, hash, CryptoStreamMode.Write, true))
                using (var writer = new BinaryWriter(cryptoStream, Encoding.Default, true))
                {
                    writer.Write((ushort)0); // file version
                    writer.Write((byte)PluginType);
                    writer.Write(GUID.ToByteArray());
                    {
                        writer.Write((byte)Name.Length);
                        writer.Write(Encoding.UTF8.GetBytes(Name));
                    }
                    {
                        writer.Write((ushort)Description.Length);
                        writer.Write(Encoding.UTF8.GetBytes(Description));
                    }
                    {
                        writer.Write((byte)Version.Length);
                        writer.Write(Encoding.UTF8.GetBytes(Version));
                    }
                    {
                        writer.Write((byte)Main.Length);
                        writer.Write(Encoding.UTF8.GetBytes(Main));
                    }
                    {
                        writer.Write((int)Icon.Length);
                        Icon.Position = 0;
                        Icon.CopyTo(writer.BaseStream);
                    }
                    {
                        writer.Write((int)DLL.Length);
                        DLL.Position = 0;
                        DLL.CopyTo(writer.BaseStream);
                    }
                    {
                        if (PDB != null)
                        {
                            writer.Write((int)PDB.Length);
                            PDB.Position = 0;
                            PDB.CopyTo(writer.BaseStream);
                        }
                        else
                        {
                            writer.Write(0); // no PDB
                        }
                    }

                    Certificate.Export(cryptoStream);
                    hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    RevocHash = hash.Hash;
                }
                using var ecdsa = new ECDsaCng(CngKey.Import(certificatePrivateKey, CngKeyBlobFormat.EccPrivateBlob));

                PluginHash = ecdsa.SignHash(RevocHash);
                isDirty = false;
            }
        }

        public async Task<VerifyError> VerifyAsync(Certificate parentCert, Client client)
        {
            if (!Verified)
            {
                throw new InvalidOperationException("Cannot verify a non-verified plugin");
            }
            if (isDirty)
            {
                throw new InvalidOperationException("Update plugin hashes before verifying");
            }
            var certVerify = await Certificate.VerifyAsync(parentCert, client);

            if (certVerify != VerifyError.SUCCESS)
                return certVerify;

            using var ecdsa = new ECDsaCng(CngKey.Import(Certificate.PublicKey, CngKeyBlobFormat.EccPublicBlob));
            return ecdsa.VerifyHash(RevocHash, PluginHash) ?
                VerifyError.SUCCESS :
                VerifyError.INVALID;
        }

        public void Export(Stream stream)
        {
            if (isDirty && Verified)
            {
                throw new InvalidOperationException("Update plugin hashes before exporting");
            }
            using var writer = new BinaryWriter(stream, Encoding.Default, true);

            writer.Write(Verified ? MAGIC_VERIFIED : MAGIC);
            writer.Write((ushort)0); // file version
            writer.Write((byte)PluginType);
            writer.Write(GUID.ToByteArray());
            {
                writer.Write((byte)Name.Length);
                writer.Write(Encoding.UTF8.GetBytes(Name));
            }
            {
                writer.Write((ushort)Description.Length);
                writer.Write(Encoding.UTF8.GetBytes(Description));
            }
            {
                writer.Write((byte)Version.Length);
                writer.Write(Encoding.UTF8.GetBytes(Version));
            }
            {
                writer.Write((byte)Main.Length);
                writer.Write(Encoding.UTF8.GetBytes(Main));
            }
            {
                writer.Write((int)Icon.Length);
                Icon.Position = 0;
                Icon.CopyTo(writer.BaseStream);
            }
            {
                writer.Write((int)DLL.Length);
                DLL.Position = 0;
                DLL.CopyTo(writer.BaseStream);
            }
            {
                if (PDB != null)
                {
                    writer.Write((int)PDB.Length);
                    PDB.Position = 0;
                    PDB.CopyTo(writer.BaseStream);
                }
                else
                {
                    writer.Write(0); // no PDB
                }
            }
            if (Verified)
            {
                Certificate.Export(stream);

                writer.Write((ushort)PluginHash.Length);
                writer.Write(PluginHash);
            }
        }

        public enum Type : byte
        {
            MODULE,
            PLATFORM
        }
    }
}
