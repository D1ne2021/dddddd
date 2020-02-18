using System;
using System.Collections.ObjectModel;
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
using fnbot.shop.Signer;
using Microsoft.Win32;
using PakReader.Parsers.Objects;
using Localization = fnbot.shop.Fortnite.Localization;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PluginManager Backend;
        BitmapImageCache ImageCache = new BitmapImageCache();
        UpdateChecker Updates;
        ObservableCollection<ConfigRowItem<IPlatform>> PlatformCollection = new ObservableCollection<ConfigRowItem<IPlatform>>();
        ObservableCollection<ConfigRowItem<IModule>> ModuleCollection = new ObservableCollection<ConfigRowItem<IModule>>();

        public MainWindow()
        {
            Backend = new PluginManager(".");
            UIUtility.Initialize((text, caption, button, image, result) => (ResultType)MessageBox.Show(text, caption == null ? "fnbot.shop" : $"{caption} - fnbot.shop", (MessageBoxButton)button, (MessageBoxImage)image, (MessageBoxResult)result));
            Task.Run(Backend.Load).GetAwaiter().GetResult();
            Backend.Run();
            foreach(var module in Backend.GetModules())
            {
                ModuleCollection.Add(new ConfigRowItem<IModule>(module, ImageCache, false, c => { }));
            }
            foreach (var platform in Backend.GetPlatforms())
            {
                PlatformCollection.Add(new ConfigRowItem<IPlatform>(platform, ImageCache, false, c => { }));
            }

            InitializeComponent();
            Updates = new UpdateChecker(UpdateEvent);

            PlatformsGrid.ItemsSource = PlatformCollection;
            ModulesGrid.ItemsSource = ModuleCollection;
            PlatformsVerifiedCol.FontFamily = PlatformsSupportsCol.FontFamily = ModulesVerifiedCol.FontFamily = new FontFamily(new Uri("pack://application:,,,/resources/"), "./#Font Awesome 5 Free Solid");
        }
        
        private void UnselectedModule(object sender, RoutedEventArgs e) // runs first
        {
            ConfigPanel.Children.Clear();
            Backend.SaveConfig(((ConfigRowItem<IModule>)((DataGridRow)sender).DataContext).Data);
        }

        private void UnselectedPlatform(object sender, RoutedEventArgs e)
        {
            ConfigPanel.Children.Clear();
            Backend.SaveConfig(((ConfigRowItem<IPlatform>)((DataGridRow)sender).DataContext).Data);
        }

        private void SelectedModule(object sender, RoutedEventArgs e) // runs next
        {
            Selected(((ConfigRowItem<IModule>)((DataGridRow)sender).DataContext).Data);
        }

        private void SelectedPlatform(object sender, RoutedEventArgs e)
        {
            Selected(((ConfigRowItem<IPlatform>)((DataGridRow)sender).DataContext).Data);
        }

        private void Selected<T>(PluginData<T> item) where T : IPlugin
        {
            if (item.PluginInstance.Config == null)
                return;
            Backend.LoadConfig(item);
            var type = item.PluginInstance.Config.GetType();
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
                    switch (field.GetValue(item.PluginInstance.Config))
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
                        case ConfigProperty<Func<IPlugin, Task>> task:
                            vis = task.Visible ? Visibility.Visible : Visibility.Collapsed;
                            var button = new Button { Content = task.Name ?? field.Name, VerticalAlignment = VerticalAlignment.Center, Visibility = vis, IsEnabled = task.Enabled };
                            button.Click += (s, e) => task.Value(item.PluginInstance);

                            task.SetEnabledCallback = GUIAction<bool>(e => button.IsEnabled = e);
                            task.SetNameCallback = GUIAction<string>(n => button.Content = n ?? field.Name);
                            // task.SetValueCallback = GUIAction<Func<IImport, Task>>(t => button.Click -= (s, e) => task.Value(imp.Instance)); unsupported
                            task.SetVisibleCallback = GUIAction<bool>(v => button.Visibility = v ? Visibility.Visible : Visibility.Collapsed);

                            Grid.SetRow(button, i);
                            Grid.SetColumnSpan(button, 3);

                            ConfigPanel.Children.Add(button);
                            break;
                        case ConfigProperty<Action<IPlugin>> action:
                            vis = action.Visible ? Visibility.Visible : Visibility.Collapsed;
                            button = new Button { Content = action.Name ?? field.Name, VerticalAlignment = VerticalAlignment.Center, Visibility = vis, IsEnabled = action.Enabled };
                            button.Click += (s, e) => action.Value(item.PluginInstance);

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
            var imp = ((ConfigRowItem<IModule>)((DataGridRow)sender).DataContext).Data;
            new ModulePostTo(Backend, ImageCache, imp.Plugin.GUID).ShowDialog();
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
            Backend.Dispose();
            Updates.Dispose();
            ModulesGrid.UnselectAll();
            PlatformsGrid.UnselectAll();
        }

        Action<T> GUIAction<T>(Action<T> a)
        {
            return t => Dispatcher.Invoke(a, t);
        }
    }
}