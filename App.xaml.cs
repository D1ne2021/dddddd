using System;
using System.Windows;
using Sentry;

namespace fnbot.shop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        IDisposable Sentry;
        const string SENTRY_DSN = "https://9ea24c65df5c4b149f24c44fe6e1803c@sentry.io/1852007";
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Sentry = SentrySdk.Init(o =>
            {
                o.Dsn = new Dsn(SENTRY_DSN);
                o.Debug = true;
                o.Release = $"{UpdateChecker.BUILD} - {UpdateChecker.VERSION}";
                o.Environment = UpdateChecker.CHANNEL.ToString();
                o.AddInAppInclude("System.");
                o.AddInAppInclude("Microsoft.");
                o.AddInAppInclude("MS");
                o.AddInAppInclude("Newtonsoft.Json");
            });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Sentry.Dispose();
        }
    }
}
