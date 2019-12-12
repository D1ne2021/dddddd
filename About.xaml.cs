using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        readonly UpdateChecker Checker;
        public About(UpdateChecker checker)
        {
            Checker = checker;
            InitializeComponent();
            Build.Text = UpdateChecker.BUILD.ToString();
            Channel.Text = UpdateChecker.CHANNEL.ToString();
            Version.Text = UpdateChecker.VERSION;
        }

        private void Click_Name(object sender, MouseButtonEventArgs e)
        {
            OpenURL("https://fnbot.shop");
        }

        private void Click_Changelog(object sender, MouseButtonEventArgs e)
        {
            new Changelog(Checker).ShowDialog();
        }

        private void Click_Twitter(object sender, MouseButtonEventArgs e)
        {
            OpenURL("https://twitter.com/Asriel_Dev");
        }

        private void Click_License(object sender, MouseButtonEventArgs e)
        {
            OpenURL("https://github.com/WorkingRobot/fnbot.shop/tree/master/LICENSE");
        }

        // visit https://stackoverflow.com/a/43232486/5662232 for multiplatform support
        public static void OpenURL(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }
    }
}
