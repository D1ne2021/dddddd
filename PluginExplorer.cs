using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using fnbot.shop.Backend;
using fnbot.shop.Web;

namespace fnbot.shop
{
    class PluginExplorer
    {
        const string LIST_PLUGINS = "https://fnbot.shop/api/plugins/list";
        const string DOWNLOAD_PLUGIN = "https://fnbot.shop/api/plugins/data/";

        readonly Client Client = new Client();

        static JsonSerializerOptions PluginSerializer;
        public async Task<Plugin[]> GetPluginsAsync(ImportType type)
        {
            if (PluginSerializer == null)
            {
                PluginSerializer = new JsonSerializerOptions();
                PluginSerializer.Converters.Add(new JsonStringEnumConverter());
            }
            return ConvertListing(await JsonSerializer.DeserializeAsync<Dictionary<string, PluginResp>>((await Client.SendAsync("GET", $"{LIST_PLUGINS}?type={type}")).Stream, PluginSerializer));
        }

        public Task InstallPlugin(Plugin plugin) => InstallPlugin(plugin.Guid, plugin.Build, plugin.Id);

        public Task InstallPlugin(Guid guid, int build, string id)
        {
            var buildId = GetBuildId(guid, build);
            return Task.WhenAll(
            Client.SendAsync("GET", $"{DOWNLOAD_PLUGIN}{buildId}.dll").ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine(t.Exception);
                }
                using (var fs = File.Open(MainWindow.PLUGIN_PATH + id + ".dll", FileMode.Create, FileAccess.Write))
                using (t.Result)
                {
                    await t.Result.Stream.CopyToAsync(fs);
                }
            }),
            Client.SendAsync("GET", $"{DOWNLOAD_PLUGIN}{buildId}.pdb").ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine(t.Exception);
                }
                using (var fs = File.Open(MainWindow.PLUGIN_PATH + id + ".pdb", FileMode.Create, FileAccess.Write))
                using (t.Result)
                {
                    await t.Result.Stream.CopyToAsync(fs);
                }
            }));
        }

        static string GetBuildId(Guid guid, int buildId)
        {
            using var sha = SHA256.Create();
            using var s = new MemoryStream(16 + 4);
            s.Write(guid.ToByteArray());
            s.Write(BitConverter.GetBytes(buildId));
            return ToHex(sha.ComputeHash(s));
        }

        static PluginExplorer()
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
            var result = new char[64];
            for (int i = 0; i < 20; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        static Plugin[] ConvertListing(IReadOnlyDictionary<string, PluginResp> dic)
        {
            var resp = new Plugin[dic.Count];
            int i = 0;
            foreach(var (id, pl) in dic)
            {
                resp[i++] = new Plugin(id, pl.name, pl.verified, pl.version, pl.build, pl.supports, pl.guid);
            }
            return resp;
        }

        public readonly struct Plugin
        {
            public string Id { get; }
            public string Name { get; }
            public bool Verified { get; }
            public string Version { get; }
            public int Build { get; }
            public ItemType[] Supports { get; }
            public Guid Guid { get; }
            public string Icon => $"https://fnbot.shop/api/plugins/icons/{Guid}.png";

            public Plugin(string id, string name, bool verified, string version, int build, ItemType[] supports, Guid guid)
            {
                Id = id;
                Name = name;
                Verified = verified;
                Version = version;
                Build = build;
                Supports = supports;
                Guid = guid;
            }
        }

        struct PluginResp
        {
            public string name { get; set; }
            public bool verified { get; set; }
            public string version { get; set; }
            public int build { get; set; }
            public ItemType[] supports { get; set; }
            public Guid guid { get; set; }
        }
    }
}
