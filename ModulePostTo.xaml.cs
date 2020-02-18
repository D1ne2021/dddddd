using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using fnbot.shop.Backend;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for ModulePostTo.xaml
    /// </summary>
    public partial class ModulePostTo : Window
    {
        readonly ObservableCollection<ConfigRowItem<IPlatform>> Rows = new ObservableCollection<ConfigRowItem<IPlatform>>();
        internal ModulePostTo(PluginManager backend, BitmapImageCache cache, Guid moduleGuid)
        {
            InitializeComponent();
            VerifiedCol.FontFamily = new FontFamily(new Uri("pack://application:,,,/resources/"), "./#Font Awesome 5 Free Solid");
            var modData = backend.GetModule(moduleGuid);
            Platforms.ItemsSource = Rows;
            foreach(var platformData in backend.GetPlatforms())
            {
                Rows.Add(new ConfigRowItem<IPlatform>(platformData, cache, backend.UsesPlatform(modData, platformData), v => _ = v ? backend.ConnectPlatform(modData, platformData) : backend.DisconnectPlatform(modData, platformData)));
            }
        }
    }

    public sealed class ConfigRowItem<T> where T : IPlugin
    {
        public PluginData<T> Data { get; }
        public string Name => Data.Plugin.Name;
        public BitmapImage Icon { get; }
        public string Verified => Data.Plugin.Verified ? "\uf8e5" : ""; // twitter svg (https://commons.wikimedia.org/wiki/File:Twitter_Verified_Badge.svg)
        public string Description => Data.Plugin.Description;
        public string Size { get; }
        public string Version => Data.Plugin.Version;
        bool checked_;
        public bool Checked { get => checked_; set { Console.WriteLine("oi " + value); CheckAction(value); checked_ = value; } }
        Action<bool> CheckAction;

        internal ConfigRowItem(PluginData<T> data, BitmapImageCache cache, bool isChecked, Action<bool> checkAction)
        {
            Data = data;
            CheckAction = checkAction;
            checked_ = isChecked;
            Size = GetBytesReadable((ulong)(data.Plugin.DLL.Length + (data.Plugin.PDB?.Length ?? 0) + data.Plugin.Icon.Length));
            Icon = cache.GetImage(data);
        }

        static string GetBytesReadable(ulong i)
        {
            string suffix;
            double readable;
            if (i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = i >> 20;
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = i >> 10;
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            readable /= 1024;
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
        }
    }
}
