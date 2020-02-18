using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using fnbot.shop.Web;

namespace fnbot.shop.Backend
{
    public sealed class PluginManager : IDisposable
    {
        public string BasePath { get; }
        public string PluginPath => Path.Combine(BasePath, "plugins");
        public string ConfigPath => Path.Combine(BasePath, "configs");

        HashSet<PluginData<IModule>> Modules = new HashSet<PluginData<IModule>>();
        HashSet<PluginData<IPlatform>> Platforms = new HashSet<PluginData<IPlatform>>();

        Dictionary<PluginData<IModule>, HashSet<PluginData<IPlatform>>> Connections = new Dictionary<PluginData<IModule>, HashSet<PluginData<IPlatform>>>();

        public bool LoopRunning => CancelToken != null && !CancelToken.IsCancellationRequested;
        CancellationTokenSource CancelToken;
        Task RunTask;

        public PluginManager(string basePath)
        {
            BasePath = basePath;
        }

        // Load from config and files

        public async Task Load()
        {
            Directory.CreateDirectory(PluginPath);
            Directory.CreateDirectory(ConfigPath);

            await LoadPlugins();
            LoadConnections();
        }

        async Task LoadPlugins()
        {
            using var client = new Client();
            foreach (var file in Directory.EnumerateFiles(PluginPath, "*.fnp"))
            {
                using var s = File.OpenRead(file);
                var plugin = new Plugin(s);
                if (await VerifyPluginAsync(plugin, client) == Certificate.VerifyError.SUCCESS)
                {
                    var assembly = LoadPlugin(plugin);
                    var mainType = assembly.GetType(plugin.Main, false, true);

                    if (mainType != null && GetPluginType(plugin.PluginType).IsAssignableFrom(mainType))
                    {
                        var pluginInstance = (IPlugin)Activator.CreateInstance(mainType);
                        switch (pluginInstance)
                        {
                            case IModule module:
                                Modules.Add(new PluginData<IModule>(assembly, plugin, module));
                                break;
                            case IPlatform platform:
                                Platforms.Add(new PluginData<IPlatform>(assembly, plugin, platform));
                                break;
                        }
                    }
                }
            }
        }

        void LoadConnections()
        {
            var connPath = Path.Join(BasePath, "connections");
            if (!File.Exists(connPath))
            {
                return;
            }
            using var s = File.OpenRead(connPath);
            using var reader = new BinaryReader(s);

            if (reader.ReadUInt16() != 0) // invalid file version
                return;
            var connCount = reader.ReadUInt16();
            for (ushort i = 0; i < connCount; ++i)
            {
                var module = GetPluginByGuid(new Guid(reader.ReadBytes(16)), Modules);
                var moduleConnCount = reader.ReadUInt16();
                if (module == null)
                {
                    reader.BaseStream.Seek(16 * moduleConnCount, SeekOrigin.Current); // module doesn't exist, we'll just skip it
                    continue;
                }
                var moduleConns = new HashSet<PluginData<IPlatform>>();
                for (ushort j = 0; j < moduleConnCount; ++j)
                {
                    var platform = GetPluginByGuid(new Guid(reader.ReadBytes(16)), Platforms);
                    if (platform != null)
                    {
                        moduleConns.Add(platform);
                    }
                }
                Connections[module] = moduleConns;
            }
        }

        void SaveConnections()
        {
            var connPath = Path.Join(BasePath, "connections");
            using var s = File.Open(connPath, FileMode.Create);
            using var writer = new BinaryWriter(s);

            writer.Write((ushort)0); // file version
            writer.Write((ushort)Connections.Count);
            foreach (var (module, connections) in Connections)
            {
                writer.Write(module.Plugin.GUID.ToByteArray());
                writer.Write((ushort)connections.Count);
                foreach (var platform in connections)
                {
                    writer.Write(platform.Plugin.GUID.ToByteArray());
                }
            }
        }

        // Config Saving/Loading

        public void LoadConfig<T>(PluginData<T> plugin) where T : IPlugin
        {
            if (plugin.PluginInstance.Config == null)
                return;
            var path = Path.Combine(ConfigPath, plugin.Plugin.GUID.ToString("N"));
            if (!File.Exists(path))
                return;

            using var fileStream = File.OpenRead(path);
            plugin.PluginInstance.Config.LoadConfig(fileStream);
            Console.WriteLine("loaded " + plugin.Plugin.Name);
        }

        public void SaveConfig<T>(PluginData<T> plugin) where T : IPlugin
        {
            if (plugin.PluginInstance.Config == null)
                return;
            var path = Path.Combine(ConfigPath, plugin.Plugin.GUID.ToString("N"));

            using var fileStream = File.Open(path, FileMode.Create); // creates and truncates the file
            plugin.PluginInstance.Config.SaveConfig(fileStream);
            Console.WriteLine("saved " + plugin.Plugin.Name);
        }

        // Run thread

        public void Run()
        {
            if (LoopRunning)
            {
                return;
            }
            CancelToken = new CancellationTokenSource();
            RunTask = RunLoopAsync();
        }
        
        public bool StopRun()
        {
            if (CancelToken == null)
            {
                return false;
            }
            if (!CancelToken.IsCancellationRequested)
            {
                CancelToken.Cancel();
            }
            return true;
        }

        async Task RunLoopAsync()
        {
            Console.WriteLine("starting loop");
            long i = 0;
            while (!CancelToken.IsCancellationRequested)
            {
                RunOneLoop(i++);
                await Task.Delay(1000); // make this delay customizable in the future
            }
            CancelToken.Dispose();
        }

        void RunOneLoop(long index)
        {
            Console.WriteLine("running loop " + index);
            foreach (var kv in Connections)
            {
                if (index % kv.Key.PluginInstance.RefreshTime == 0)
                {
                    Console.WriteLine("posting "+kv.Key.Plugin.Name);
                    var postTask = kv.Key.PluginInstance.Post(false);
                    _ = postTask.ContinueWith(async t =>
                    {
                        Console.WriteLine("error " + kv.Key.Plugin.Name);
                        // error when getting post data for module
                    }, TaskContinuationOptions.OnlyOnFaulted);
                    _ = postTask.ContinueWith(t =>
                    {
                        if (t.Result != null)
                        {
                            foreach (var platform in kv.Value)
                            {
                                var platformTask = platform.PluginInstance.PostItem(t.Result);
                                _ = platformTask.ContinueWith(async t =>
                                {
                                    // error when posting to platform
                                }, TaskContinuationOptions.OnlyOnFaulted);
                            }
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
                }
            }
        }

        // Verification

        static Dictionary<Plugin.Type, Certificate> CertificateCache = new Dictionary<Plugin.Type, Certificate>();
        static async Task<Certificate> GetCertificateAsync(Plugin.Type type, Client client)
        {
            if (!CertificateCache.TryGetValue(type, out var ret))
            {
                using var resp = await client.SendAsync("GET", $"https://fnbot.shop/api/plugincert?t={type:X}");
                ret = new Certificate(resp.Stream);
                CertificateCache[type] = ret;

                switch (await ret.VerifyAsync(await GetCertificateAsync((Plugin.Type)0xFF, client), client))
                {
                    case Certificate.VerifyError.INVALID:
                        throw new SecurityException("Parent certificate is invalid");
                    case Certificate.VerifyError.EXPIRED:
                        throw new SecurityException("Parent certificate is expired");
                    case Certificate.VerifyError.REVOKED:
                        throw new SecurityException("Parent certificate is revoked");
                }
            }
            return ret;
        }
        static async Task<Certificate.VerifyError> VerifyPluginAsync(Plugin p, Client client)
        {
            if (p.Verified)
            {
                var result = await p.VerifyAsync(await GetCertificateAsync(p.PluginType, client), client);
                if (result != Certificate.VerifyError.SUCCESS)
                    return result;
                if (p.Name != p.Certificate.Name)
                    return Certificate.VerifyError.INVALID;
                return Certificate.VerifyError.SUCCESS;
            }
            return Certificate.VerifyError.SUCCESS;
        }

        // Loading

        static Assembly LoadPlugin(Plugin p)
        {
            p.DLL.Position = 0;
            if (p.PDB != null)
            {
                p.PDB.Position = 0;
                return AssemblyLoadContext.Default.LoadFromStream(p.DLL, p.PDB);
            }
            else
            {
                return AssemblyLoadContext.Default.LoadFromStream(p.DLL);
            }
        }

        // Helper Methods

        static Type GetPluginType(Plugin.Type type) =>
            type switch
            {
                Plugin.Type.MODULE => typeof(IModule),
                Plugin.Type.PLATFORM => typeof(IPlatform),
                _ => null,
            };

        static PluginData<T> GetPluginByGuid<T>(Guid guid, HashSet<PluginData<T>> set) where T : IPlugin
        {
            foreach (var data in set)
            {
                if (data.Plugin.GUID == guid)
                    return data;
            }
            return null;
        }

        public PluginData<IModule> GetModule(Guid guid) =>
            GetPluginByGuid(guid, Modules);

        public PluginData<IPlatform> GetPlatform(Guid guid) =>
            GetPluginByGuid(guid, Platforms);

        public IEnumerable<PluginData<IModule>> GetModules() => Modules;

        public IEnumerable<PluginData<IPlatform>> GetPlatforms() => Platforms;

        public bool UsesPlatform(PluginData<IModule> module, PluginData<IPlatform> platform) =>
            Connections.TryGetValue(module, out var set) ?
                set.Contains(platform) :
                false;

        public bool ConnectPlatform(PluginData<IModule> module, PluginData<IPlatform> platform)
        {
            if (!Connections.TryGetValue(module, out var set))
            {
                Connections[module] = set = new HashSet<PluginData<IPlatform>>();
            }
            return set.Add(platform);
        }

        public bool DisconnectPlatform(PluginData<IModule> module, PluginData<IPlatform> platform) =>
            Connections.TryGetValue(module, out var set) ?
                   set.Remove(platform) :
                   false;

        // Disposer Method

        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            StopRun();
            foreach(var module in Modules)
            {
                SaveConfig(module);
            }
            foreach (var platform in Platforms)
            {
                SaveConfig(platform);
            }
            SaveConnections();
        }
    }

    public class PluginData<T> where T : IPlugin // I'd make it readonly struct, but value types don't work well if you want to implement connections :)
    {
        public readonly Assembly Assembly;
        public readonly Plugin Plugin;
        public readonly T PluginInstance;

        internal PluginData(Assembly assembly, Plugin plugin, T pluginInst)
        {
            Assembly = assembly;
            Plugin = plugin;
            PluginInstance = pluginInst;
        }
    }
}
