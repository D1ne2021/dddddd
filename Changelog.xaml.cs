using System.Windows;
using System.Windows.Input;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for Changelog.xaml
    /// </summary>
    public partial class Changelog : Window
    {
        public Changelog(UpdateChecker checker)
        {
            InitializeComponent();
            checker.GetPatchNotesAsync().ContinueWith((t) => Dispatcher.Invoke(() => TextData.Text = t.Result));
        }

        private void Click_Name(object sender, MouseButtonEventArgs e)
        {
            About.OpenURL("https://fnbot.shop");
        }
    }
}
