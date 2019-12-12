using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using fnbot.shop.Backend;
using fnbot.shop.Backend.Configuration;
using fnbot.shop.Fortnite;
using Microsoft.Win32;
using Localization = fnbot.shop.Fortnite.Localization;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackendManager Backend;
        UpdateChecker Updates;
        PluginExplorer Explorer;
        internal const string PLUGIN_PATH = ".\\Plugins\\";
        const string CONFIG_PATH = ".\\Configs\\";
        internal const string CREDS_PATH = "creds.dat";
        internal const string TOKEN_PATH = "token.dat";
        internal const string FORTNITE_PATH = "fn.dat";

        public MainWindow()
        {
            UIUtility.Initialize((text, caption, button, image, result) => (ResultType)MessageBox.Show(text, caption == null ? "fnbot.shop" : $"{caption} - fnbot.shop", (MessageBoxButton)button, (MessageBoxImage)image, (MessageBoxResult)result));
            if (!File.Exists(CREDS_PATH))
            {
                new Setup().ShowDialog();
            }
            Directory.CreateDirectory(PLUGIN_PATH);
            Directory.CreateDirectory(CONFIG_PATH);
            Dispatcher.Invoke(async () => {
                try
                {
                    await AuthSupplier.Login(CREDS_PATH, TOKEN_PATH);
                    await AuthSupplier.GetKeychainAsync();
                    await PakSupplier.InitializeOffline(File.ReadAllText(FORTNITE_PATH));
                    Localization.Initialize();

                    ImportManager.LoadImports(PLUGIN_PATH, CONFIG_PATH);
                    foreach (var m in ImportManager.ModuleCollection)
                    {
                        Backend.AddModule(m.Import.GUID, (IModule)m.Import.Instance);
                    }
                    foreach (var p in ImportManager.PlatformCollection)
                    {
                        Backend.AddPlatform(p.Import.GUID, (IPlatform)p.Import.Instance);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            InitializeComponent();
            Backend = new BackendManager();
            Updates = new UpdateChecker(UpdateEvent);
            Explorer = new PluginExplorer();

            PlatformsGrid.ItemsSource = ImportManager.PlatformCollection;
            ModulesGrid.ItemsSource = ImportManager.ModuleCollection;
            PlatformsVerifiedCol.FontFamily = PlatformsSupportsCol.FontFamily = ModulesVerifiedCol.FontFamily = ModulesSupportsCol.FontFamily = new FontFamily(new Uri("pack://application:,,,/resources/"), "./#Font Awesome 5 Free Solid");
        }
        
        private void Unselected(object sender, RoutedEventArgs e) // runs first
        {
            Unselected(((ConfigRowItem)((DataGridRow)sender).DataContext).Import);
        }

        void Unselected(ImportManager.Import imp)
        {
            ConfigPanel.Children.Clear();
            if (imp.Instance.Config == null)
                return;
            using (var s = File.Open(CONFIG_PATH + imp.GUID, FileMode.Create, FileAccess.Write))
            {
                imp.Instance.Config.SaveConfig(s);
                Console.WriteLine("saved " + imp.Name);
            }
        }

        private void Selected(object sender, RoutedEventArgs e) // runs next
        {
            var imp = ((ConfigRowItem)((DataGridRow)sender).DataContext).Import;
            if (imp.Instance.Config == null)
                return;
            if (File.Exists(CONFIG_PATH + imp.GUID))
            {
                try
                {
                    using (var s = File.OpenRead(CONFIG_PATH + imp.GUID))
                    {
                        imp.Instance.Config.LoadConfig(s);
                        Console.WriteLine("loaded " + imp.Name);
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Could not load " + imp.Name);
                    Console.WriteLine(exc);
                }
            }
            var type = imp.Instance.Config.GetType();
            PakConfigAttribute pakAttr;
            {
                var attrs = type.GetCustomAttributes(typeof(PakConfigAttribute), true);
                if (attrs.Length != 0)
                    pakAttr = (PakConfigAttribute)attrs[0];
            }
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                int i = 0;
                foreach(var field in fields)
                {
                    if (!field.IsInitOnly || !typeof(ConfigProperty).IsAssignableFrom(field.FieldType))
                        continue;
                    ConfigPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    ConfigPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5) });
                    switch (field.GetValue(imp.Instance.Config))
                    {
                        case ConfigProperty<string> str:
                            var vis = str.Visible ? Visibility.Visible : Visibility.Collapsed;
                            var block = new TextBlock { Text = str.Name ?? field.Name, Visibility = vis, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                            var box = new TextBox {VerticalAlignment = VerticalAlignment.Center, IsEnabled = str.Enabled, Visibility = vis, Text = str.Value };
                            box.TextChanged += (s, e) => str.UISetValue(box.Text);

                            str.SetEnabledCallback = GUIAction<bool>(e => box.IsEnabled = e);
                            str.SetNameCallback = GUIAction<string>(n => block.Text = n ?? field.Name);
                            str.SetValueCallback = GUIAction<string>(t => box.Text = t);
                            str.SetVisibleCallback = GUIAction<bool>(v => { var vis = v ? Visibility.Visible : Visibility.Collapsed; block.Visibility = vis; box.Visibility = vis; });

                            Grid.SetRow(block, i);
                            Grid.SetRow(box, i);
                            Grid.SetColumn(box, 2);

                            ConfigPanel.Children.Add(block);
                            ConfigPanel.Children.Add(box);
                            break;
                        case ConfigProperty<StringLabel> label:
                            vis = label.Visible ? Visibility.Visible : Visibility.Collapsed;
                            block = new TextBlock { Text = label.Name ?? field.Name, Visibility = vis, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                            var txt = new TextBlock { Text = label.Value.Value, Visibility = vis, VerticalAlignment = VerticalAlignment.Center };

                            label.SetValueCallback = t => txt.Text = t.Value;
                            label.SetNameCallback = GUIAction<string>(n => block.Text = n ?? field.Name);
                            label.SetValueCallback = GUIAction<StringLabel>(t => txt.Text = t.Value);
                            label.SetVisibleCallback = GUIAction<bool>(v => { var vis = v ? Visibility.Visible : Visibility.Collapsed; block.Visibility = vis; txt.Visibility = vis; });

                            Grid.SetRow(block, i);
                            Grid.SetRow(txt, i);
                            Grid.SetColumn(txt, 2);

                            ConfigPanel.Children.Add(block);
                            ConfigPanel.Children.Add(txt);
                            break;
                        case ConfigProperty<Func<IImport, Task>> task:
                            vis = task.Visible ? Visibility.Visible : Visibility.Collapsed;
                            var button = new Button { Content = task.Name ?? field.Name, VerticalAlignment = VerticalAlignment.Center, Visibility = vis, IsEnabled = task.Enabled };
                            button.Click += (s, e) => task.Value(imp.Instance);

                            task.SetEnabledCallback = GUIAction<bool>(e => button.IsEnabled = e);
                            task.SetNameCallback = GUIAction<string>(n => button.Content = n ?? field.Name);
                            // task.SetValueCallback = GUIAction<Func<IImport, Task>>(t => button.Click -= (s, e) => task.Value(imp.Instance)); unsupported
                            task.SetVisibleCallback = GUIAction<bool>(v => button.Visibility = v ? Visibility.Visible : Visibility.Collapsed);

                            Grid.SetRow(button, i);
                            Grid.SetColumnSpan(button, 3);

                            ConfigPanel.Children.Add(button);
                            break;
                        case ConfigProperty<Action<IImport>> action:
                            vis = action.Visible ? Visibility.Visible : Visibility.Collapsed;
                            button = new Button { Content = action.Name ?? field.Name, VerticalAlignment = VerticalAlignment.Center, Visibility = vis, IsEnabled = action.Enabled };
                            button.Click += (s, e) => action.Value(imp.Instance);

                            action.SetEnabledCallback = GUIAction<bool>(e => button.IsEnabled = e);
                            action.SetNameCallback = GUIAction<string>(n => button.Content = n ?? field.Name);
                            // action.SetValueCallback = GUIAction<Action<IImport>>(t => button.Click -= (s, e) => action.Value(imp.Instance)); unsupported
                            action.SetVisibleCallback = GUIAction<bool>(v => button.Visibility = v ? Visibility.Visible : Visibility.Collapsed);

                            Grid.SetRow(button, i);
                            Grid.SetColumnSpan(button, 3);

                            ConfigPanel.Children.Add(button);
                            break;
                        case ConfigProperty<TextArea> textArea:
                            vis = textArea.Visible ? Visibility.Visible : Visibility.Collapsed;
                            block = new TextBlock { Text = textArea.Name ?? field.Name, Visibility = vis, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                            box = new TextBox { VerticalAlignment = VerticalAlignment.Center, IsEnabled = textArea.Enabled, Visibility = vis, Text = textArea.Value.Value, Height = double.NaN, AcceptsReturn = true }; // NaN is auto
                            box.TextChanged += (s, e) => textArea.UISetValue(box.Text);

                            textArea.SetEnabledCallback = GUIAction<bool>(e => box.IsEnabled = e);
                            textArea.SetNameCallback = GUIAction<string>(n => block.Text = n ?? field.Name);
                            textArea.SetValueCallback = GUIAction<TextArea>(t => box.Text = t.Value);
                            textArea.SetVisibleCallback = GUIAction<bool>(v => { var vis = v ? Visibility.Visible : Visibility.Collapsed; block.Visibility = vis; box.Visibility = vis; });

                            Grid.SetRow(block, i);
                            Grid.SetRow(box, i);
                            Grid.SetColumn(box, 2);

                            ConfigPanel.Children.Add(block);
                            ConfigPanel.Children.Add(box);
                            break;
                        case ConfigProperty<FilePath> file:
                            vis = file.Visible ? Visibility.Visible : Visibility.Collapsed;
                            block = new TextBlock { Text = file.Name ?? field.Name, Visibility = vis, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                            txt = new TextBlock { Text = Path.GetFileName(file.Value.Path), VerticalAlignment = VerticalAlignment.Center, Visibility = vis };
                            button = new Button { Content = "Browse", VerticalAlignment = VerticalAlignment.Center, Visibility = vis, IsEnabled = file.Enabled, HorizontalAlignment = HorizontalAlignment.Right };
                            button.Click += (s, e) =>
                            {
                                var dialog = new OpenFileDialog
                                {
                                    CheckFileExists = true,
                                    DereferenceLinks = true,
                                    Multiselect = false,
                                    ValidateNames = true,
                                    Title = file.Name ?? field.Name,
                                    Filter = file.Value.Filter
                                };
                                if (dialog.ShowDialog(this) ?? false)
                                {
                                    txt.Text = dialog.SafeFileName;
                                    file.UISetValue(file.Value.ChangePath(dialog.FileName));
                                }
                            };

                            file.SetEnabledCallback = GUIAction<bool>(e => button.IsEnabled = e);
                            file.SetNameCallback = GUIAction<string>(n => block.Text = n ?? field.Name);
                            file.SetValueCallback = GUIAction<FilePath>(t => txt.Text = t.Path);
                            file.SetVisibleCallback = GUIAction<bool>(v => { var vis = v ? Visibility.Visible : Visibility.Collapsed; block.Visibility = vis; txt.Visibility = vis; button.Visibility = vis; });

                            Grid.SetRow(block, i);
                            Grid.SetRow(txt, i);
                            Grid.SetRow(button, i);
                            Grid.SetColumn(txt, 2);
                            Grid.SetColumn(button, 2);

                            ConfigPanel.Children.Add(block);
                            ConfigPanel.Children.Add(txt);
                            ConfigPanel.Children.Add(button);
                            break;
                    }
                    i += 2;
                }
            }
        }

        private void EditModulePosts(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var imp = ((ConfigRowItem)((DataGridRow)sender).DataContext).Import;
            new ModulePostTo(Backend, imp.GUID).ShowDialog();
        }

        void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                foreach (TabItem item in e.RemovedItems)
                {
                    ((DataGrid)item.Content).UnselectAll();
                }
            }
        }

        void UpdateEvent(VersionData ver)
        {
            if (UIUtility.ShowDialog($"A new update is available: {ver.version} ({ver.build})\nDownload?", "Update", ButtonType.YesNo, ImageType.Asterisk, ResultType.No) == ResultType.Yes)
            {
                About.OpenURL(ver.download);
                Close();
            }
        }

        private void Click_Updates(object sender, RoutedEventArgs e)
        {
            Updates.ForceCheck().ContinueWith(t =>
            {
                if (!t.Result)
                    UIUtility.ShowDialog("No update available, yet!", "Update", ButtonType.OK, ImageType.Information, ResultType.None);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void Click_Changelog(object sender, RoutedEventArgs e)
        {
            new Changelog(Updates).ShowDialog();
        }

        private void Click_About(object sender, RoutedEventArgs e)
        {
            new About(Updates).ShowDialog();
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            // save config
            ModulesGrid.UnselectAll();
            PlatformsGrid.UnselectAll();
        }

        Action<T> GUIAction<T>(Action<T> a)
        {
            return t => Dispatcher.Invoke(a, t);
        }
    }

    internal readonly struct ConfigRowItem
    {
        public ImportManager.Import Import { get; }
        public string Name => Import.Name;
        public BitmapImage Icon => Import.Icon;
        public string Verified { get; }
        public string Supports { get; }
        public string Version => Import.Version;

        public ConfigRowItem(ImportManager.Import import, bool verified)
        {
            Import = import;
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