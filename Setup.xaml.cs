using System;
using System.IO;
using System.Windows;
using fnbot.shop.Backend;
using fnbot.shop.Fortnite;
using WPFFolderBrowser;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : Window
    {
        public Setup()
        {
            InitializeComponent();
        }

        bool closable = false;
        private void Click_Login(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fortnitePath))
            {
                UIUtility.ShowDialog("Make sure you set your Fortnite path!", "Login", ButtonType.OK, ImageType.Error, ResultType.None);
                return;
            }
            if (string.IsNullOrWhiteSpace(User.Text))
            {
                UIUtility.ShowDialog("Username is empty!", "Login", ButtonType.OK, ImageType.Error, ResultType.None);
                return;
            }
            if (string.IsNullOrWhiteSpace(Pwd.Password))
            {
                UIUtility.ShowDialog("Password is empty!", "Login", ButtonType.OK, ImageType.Error, ResultType.None);
                return;
            }
            AuthSupplier.WriteCreds(MainWindow.CREDS_PATH, User.Text, Pwd.Password);
            File.WriteAllText(MainWindow.FORTNITE_PATH, fortnitePath);
            closable = true;
            Close();
        }

        string fortnitePath;
        private void Click_Browse(object sender, RoutedEventArgs e)
        {
            var dialog = new WPFFolderBrowserDialog("Choose Fortnite Pak Folder (FortniteGame\\Content\\Paks)")
            {
                DereferenceLinks = true
            };
            if (dialog.ShowDialog(this) ?? false)
            {
                if (Directory.Exists(dialog.FileName) && Directory.GetFiles(dialog.FileName, "*.pak").Length > 10)
                {
                    fortnitePath = dialog.FileName;
                }
                else
                {
                    UIUtility.ShowDialog("Invalid pak folder! Make sure the folder has all the pak files!", "Login", ButtonType.OK, ImageType.Error, ResultType.None);
                }
            }
        }

        private void Closing_(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!closable)
            {
                Environment.Exit(0);
            }
        }
    }
}
