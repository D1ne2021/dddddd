using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fnbot.shop.Backend;
using fnbot.shop.Backend.Configuration;
using fnbot.shop.Backend.ItemTypes;
using fnbot.shop.Fortnite;
using ItemT = fnbot.shop.Backend.ItemType;

namespace fnbot.shop.ItemShop
{
    class Module : IModule
    {
        public int RefreshTime => 5000;

        readonly ModuleConfig config = new ModuleConfig();
        public IConfig Config => config;

        readonly SHA1 sha = SHA1.Create();
        string ShopId;

        public Module()
        {
            Series.Initialize();
        }

        public void Dispose()
        {
            sha.Dispose();
        }

        public async Task<IItem> Post(bool force)
        {
            var store = await AuthSupplier.GetStorefrontAsync();
            var id = GetShopId(store);
            Console.WriteLine(id);
            if (ShopId == null)
            {
                ShopId = id;
                if (!force)
                    return null;
            }
            if (ShopId == id && !force)
                return null;
            var mem = new MemoryStream();
            try
            {
                var s = Stopwatch.StartNew();
                new Generator(store, config.Background.Value.Path).WriteToStream(mem);
                s.Stop();
                Console.WriteLine(s.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return new ItemVideo("test video!", 1280, 720, 3, 30, mem, ItemVideoInfo.MediaType.MP4);
        }

        string GetShopId(Storefront store)
        {
            var builder = new StringBuilder();
            foreach(var item in store.Daily)
            {
                builder.Append(item.OfferID);
            }
            foreach (var item in store.Weekly)
            {
                builder.Append(item.OfferID);
            }
            return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
        }

        static Module()
        {
            _Lookup32 = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                var s = i.ToString("x2");
                _Lookup32[i] = s[0] + ((uint)s[1] << 16);
            }
        }
        static readonly uint[] _Lookup32;
        static string ToHex(ReadOnlySpan<byte> bytes)
        {
            var result = new char[40];
            for (int i = 0; i < 20; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        public bool PostsType(ItemT type) =>
            type switch
            {
                ItemT.TEXT => true,
                ItemT.IMAGE => true,
                ItemT.ALBUM => true,
                ItemT.GIF => true,
                ItemT.VIDEO => true,
                _ => false,
            };
    }
}
