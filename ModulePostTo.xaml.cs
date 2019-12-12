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
        readonly ObservableCollection<ConfigRowItem> Rows = new ObservableCollection<ConfigRowItem>();
        public ModulePostTo(BackendManager backend, Guid moduleGuid)
        {
            InitializeComponent();
            VerifiedCol.FontFamily = SupportsCol.FontFamily = new FontFamily(new Uri("pack://application:,,,/resources/"), "./#Font Awesome 5 Free Solid");
            var modData = backend.GetModule(moduleGuid).Value;
            Platforms.ItemsSource = Rows;
            foreach(var guid in ImportManager.Platforms)
            {
                Rows.Add(new ConfigRowItem(ImportManager.LoadedImports[guid], true, modData.HasPlatform(guid), v => _ = v ? modData.AddPlatform(guid) : modData.RemovePlatform(guid)));
            }
        }

        internal sealed class ConfigRowItem
        {
            public ImportManager.Import Import { get; }
            public string Name => Import.Name;
            public BitmapImage Icon => Import.Icon;
            public string Verified { get; }
            public string Supports { get; }
            public string Version => Import.Version;
            bool checked_;
            public bool Checked { get => checked_; set { Console.WriteLine("oi "+value);CheckAction(value); checked_ = value; } }
            Action<bool> CheckAction;

            public ConfigRowItem(ImportManager.Import import, bool verified, bool isChecked, Action<bool> checkAction)
            {
                Import = import;
                CheckAction = checkAction;
                checked_ = isChecked;
                Verified = verified ? "\uf8e5" : ""; // twitter svg (https://commons.wikimedia.org/wiki/File:Twitter_Verified_Badge.svg)
                {
                    var supports = new char[Import.SupportedTypes.Length];
                    for (int i = 0; i < Import.SupportedTypes.Length; i++)
                    {
                        supports[i] = Import.SupportedTypes[i] switch
                        {
                            ItemType.TEXT => '\uf075', // fas fa-comment
                            ItemType.IMAGE => '\uf03e', // fas fa-image
                            ItemType.ALBUM => '\uf302', // fas fa-images
                            ItemType.GIF => '\uf8e4', // (custom gif https://www.flaticon.com/free-icon/gif_565520)
                            ItemType.VIDEO => '\uf03d', // fas fa-video
                            _ => '\0'
                        };
                    }
                    Supports = string.Join(" ", supports);
                }
            }
        }
    }
}
