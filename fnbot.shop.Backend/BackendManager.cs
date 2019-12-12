using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using fnbot.shop.Backend.ItemTypes;

namespace fnbot.shop.Backend
{
    public class BackendManager
    {
        readonly Dictionary<Guid, ModuleData> Modules = new Dictionary<Guid, ModuleData>();
        readonly Dictionary<Guid, IPlatform> Platforms = new Dictionary<Guid, IPlatform>();

        public bool AddModule(Guid guid, IModule module)
        {
            if (GetModule(guid).HasValue)
            {
                return false;
            }
            Modules[guid] = new ModuleData(this, module);
            return true;
        }

        public bool AddPlatform(Guid guid, IPlatform platform)
        {
            if (GetPlatform(guid) != null)
            {
                return false;
            }
            Platforms[guid] = platform;
            return true;
        }

        public ModuleData? GetModule(Guid guid)
        {
            return Modules.TryGetValue(guid, out var ret) ? ret : (ModuleData?)null;
        }

        public IPlatform GetPlatform(Guid guid)
        {
            return Platforms.TryGetValue(guid, out var ret) ? ret : null;
        }

        public bool RemoveModule(Guid guid)
        {
            var data = GetModule(guid);
            if (data == null)
            {
                return false;
            }
            data?.Token.Cancel();
            data?.Module.Dispose();
            Modules.Remove(guid);
            return true;
        }

        public bool RemovePlatform(Guid guid)
        {
            var platform = GetPlatform(guid);
            if (platform == null)
            {
                return false;
            }
            foreach(var module in Modules.Values)
            {
                module.RemovePlatform(guid);
            }
            platform.Dispose();
            Platforms.Remove(guid);
            return true;
        }

        public readonly struct ModuleData
        {
            public readonly BackendManager Manager;
            public readonly IModule Module;
            public readonly Task Task;
            public readonly CancellationTokenSource Token;
            public readonly HashSet<Guid> Platforms;

            internal ModuleData(BackendManager manager, IModule module)
            {
                Console.WriteLine("making module");
                Manager = manager;
                Module = module;
                Token = new CancellationTokenSource();
                Platforms = new HashSet<Guid>();
                Task = null; // this has to be here since ModuleData is a struct and I don't want the task to go rogue either
                Task = Run();
            }

            public bool HasPlatform(Guid guid) => Platforms.Contains(guid);

            public bool AddPlatform(Guid guid)
            {
                if (Platforms.Contains(guid))
                {
                    return false;
                }
                var platform = Manager.GetPlatform(guid);
                if (platform == null)
                    return false;
                Platforms.Add(guid);
                return true;
            }

            public bool RemovePlatform(Guid guid)
            {
                return Platforms.Remove(guid);
            }

            async Task Run()
            {
                Console.WriteLine("running");
                while (!Token.IsCancellationRequested)
                {
                    var s = Stopwatch.StartNew();
                    IItem i = null;
                    try
                    {
                        i = await Module.Post(false);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    s.Stop();
                    Console.WriteLine($"Ran post in {s.Elapsed.TotalMilliseconds}");
                    if (i != null)
                    {
                        foreach(var platform in Platforms)
                        {
                            _ = Manager.GetPlatform(platform).PostItem(i).ContinueWith(t=>
                            {
                                if (t.IsFaulted)
                                {
                                    Console.WriteLine("failed!");
                                    Console.WriteLine(t.Exception);
                                }
                                else if (t.IsCompletedSuccessfully)
                                {
                                    Console.WriteLine("completed!");
                                }
                            });
                        }
                    }
                    await Task.Delay(Module.RefreshTime, Token.Token);
                }
            }
        }
    }
}
