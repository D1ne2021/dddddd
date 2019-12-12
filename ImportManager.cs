using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace fnbot.shop.Backend
{
    static class ImportManager
    {
        public readonly static Dictionary<Guid, Import> LoadedImports = new Dictionary<Guid, Import>();
        public readonly static HashSet<Guid> Modules = new HashSet<Guid>();
        public readonly static HashSet<Guid> Platforms = new HashSet<Guid>();

        public readonly static ObservableCollection<ConfigRowItem> ModuleCollection = new ObservableCollection<ConfigRowItem>();
        public readonly static ObservableCollection<ConfigRowItem> PlatformCollection = new ObservableCollection<ConfigRowItem>();

        public static int LoadImports(string importPath, string configPath)
        {
            int i = 0;
            foreach(var file in Directory.EnumerateFiles(importPath, "*.dll"))
            {
                if (TryLoadImport(file, out var imp))
                {
                    i++;
                    if (imp.Instance.Config != null && File.Exists(configPath + imp.GUID))
                    {
                        try
                        {
                            using (var s = File.OpenRead(configPath + imp.GUID))
                            {
                                imp.Instance.Config.LoadConfig(s);
                                Console.WriteLine("loaded " + imp.Name);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Could not load " + imp.Name);
                        }
                    }
                }
            }
            return i;
        }

        public static bool TryLoadImport(string path, out Import imp)
        {
            try
            {
                imp = LoadImport(path);
            }
            catch(ArgumentException)
            {
                imp = null;
                return false;
            }
            return true;
        }

        public static Import LoadImport(string path)
        {
            var imp = new Import();

            if (File.Exists(Path.ChangeExtension(path, "pdb")))
                imp.Assembly = Assembly.Load(File.ReadAllBytes(path), File.ReadAllBytes(Path.ChangeExtension(path, "pdb")));
            else
                imp.Assembly = Assembly.LoadFile(path);

            imp.AssemblyName = imp.Assembly.GetName().Name;
            Config config;
            using (var configS = imp.Assembly.GetManifestResourceStream(imp.AssemblyName + ".config.json"))
            {
                if (configS == null)
                    throw new ArgumentException("No config file exists (config.json)");
                var b = new byte[configS.Length - 3]; // some kind of header?
                configS.Position += 3;
                configS.Read(b);
                config = JsonSerializer.Deserialize<Config>(b);
            }
            if (LoadedImports.ContainsKey(config.GUID))
            {
                throw new ArgumentException("Import already loaded");
            }
            imp.Main = imp.Assembly.GetType(config.Main);
            if (imp.Main == null)
            {
                throw new ArgumentException($"{config.Main} does not exist");
            }
            if (!Enum.TryParse(config.Type, out imp.Type))
            {
                throw new ArgumentException($"{config.Type} is not a valid config type");
            }
            var baseType = imp.Type switch
            {
                ImportType.MODULE => typeof(IModule),
                ImportType.PLATFORM => typeof(IPlatform),
                _ => throw new InvalidCastException("Invalid import type")
            };
            if (!baseType.IsAssignableFrom(imp.Main))
            {
                throw new ArgumentException($"{config.Main} is not of type {baseType.Name}");
            }

            var constructor = imp.Main.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new ArgumentException($"{config.Main} doesn't have a parameterless constructor");
            }
            imp.Instance = (IImport)constructor.Invoke(Array.Empty<object>());

            using (var iconS = imp.Assembly.GetManifestResourceStream(imp.AssemblyName + ".icon.png"))
            {
                if (iconS == null)
                {
                    throw new ArgumentException($"No icon image exists (icon.png)");
                }
                imp.Icon = new BitmapImage();
                imp.Icon.BeginInit();
                imp.Icon.StreamSource = iconS;
                imp.Icon.CacheOption = BitmapCacheOption.OnLoad;
                imp.Icon.EndInit();
            }

            imp.Name = config.Name;
            imp.Version = config.Version;
            imp.GUID = config.GUID;

            switch (imp.Type)
            {
                case ImportType.MODULE:
                    var mod = (IModule)imp.Instance;
                    var types = new List<ItemType>();
                    foreach(ItemType type in Enum.GetValues(typeof(ItemType)))
                    {
                        if (mod.PostsType(type))
                            types.Add(type);
                    }
                    imp.SupportedTypes = types.ToArray();
                    Modules.Add(imp.GUID);
                    ModuleCollection.Add(new ConfigRowItem(imp, true));
                    break;
                case ImportType.PLATFORM:
                    var plat = (IPlatform)imp.Instance;
                    types = new List<ItemType>();
                    foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
                    {
                        if (plat.SupportsType(type) != null)
                            types.Add(type);
                    }
                    imp.SupportedTypes = types.ToArray();
                    Platforms.Add(imp.GUID);
                    PlatformCollection.Add(new ConfigRowItem(imp, true));
                    break;
            }

            LoadedImports[imp.GUID] = imp;
            return imp;
        }

        public class Import
        {
            public Assembly Assembly;
            public string AssemblyName;
            public string Name;
            public string Version;
            public ItemType[] SupportedTypes;
            public Guid GUID;
            public Type Main;
            public IImport Instance;
            public ImportType Type;
            public BitmapImage Icon;
        }

        class Config
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("version")]
            public string Version { get; set; }
            [JsonPropertyName("guid")]
            public Guid GUID { get; set; }
            [JsonPropertyName("main")]
            public string Main { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }
        }
    }

    public enum ImportType
    {
        MODULE,
        PLATFORM
    }
}
