using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using fnbot.shop.Backend;
using fnbot.shop.Backend.Configuration;

namespace fnbot.shop.Twitter
{
    class PlatformConfig : IConfig
    {
        private string PINSecret;
        private string PINToken;
        public readonly ConfigProperty<Func<IPlugin,Task>> AuthPin;
        public readonly ConfigProperty<string> TwitterPin;
        public readonly ConfigProperty<Func<IPlugin, Task>> SubmitPin;
        public readonly ConfigProperty<Action<IPlugin>> Logout;
        public readonly ConfigProperty<StringLabel> AuthStatus;
        public PlatformConfig()
        {
            AuthPin = new ConfigProperty<Func<IPlugin, Task>>(AuthPinF, "(1) Authorize with PIN", true, true);
            TwitterPin = new ConfigProperty<string>(null, "(2) Enter PIN from Twitter", false, false);
            SubmitPin = new ConfigProperty<Func<IPlugin, Task>>(SubmitPinF, "(3) Submit PIN", false, false);
            Logout = new ConfigProperty<Action<IPlugin>>(LogoutF, "Log Out", true, false);
            AuthStatus = new ConfigProperty<StringLabel>("Not Logged In", "Status", false, true);
        }
        public async Task AuthPinF(IPlugin import)
        {
            AuthPin.Enabled = false;
            _ = Task.Delay(5000).ContinueWith(t => AuthPin.Enabled = true);
            var platform = import as Platform;
            var resp = await platform.Client.SendJsonAsync("POST", "https://fnbot.shop/api/twitreq", "{\"method\": \"POST\",\"url\": \"https://api.twitter.com/oauth/request_token\",\"params\": { \"oauth_callback\": \"oob\"}}");
            platform.Client.SetHeader("Authorization", await resp.GetStringAsync());
            resp = await platform.Client.SendAsync("POST", "https://api.twitter.com/oauth/request_token");
            var callbackData = HttpUtility.ParseQueryString(await resp.GetStringAsync());
            if (callbackData["oauth_callback_confirmed"] == "true")
            {
                PINToken = callbackData["oauth_token"];
                PINSecret = callbackData["oauth_token_secret"];
            }
            else
                throw new InvalidProgramException("Oauth callback was not confirmed");
            OpenURL($"https://api.twitter.com/oauth/authorize?oauth_token={PINToken}");
            SubmitPin.Enabled = SubmitPin.Visible = TwitterPin.Enabled = TwitterPin.Visible = true;
        }
        // visit https://stackoverflow.com/a/43232486/5662232 for multiplatform support
        static void OpenURL(string url)
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

        internal string OAuthToken;
        internal string OAuthSecret;
        string ScreenName;
        public async Task SubmitPinF(IPlugin import)
        {
            SubmitPin.Enabled = TwitterPin.Enabled = false;
            _ = Task.Delay(5000).ContinueWith(t => SubmitPin.Enabled = TwitterPin.Enabled = true);
            var platform = import as Platform;
            var resp = await platform.Client.SendJsonAsync("POST", "https://fnbot.shop/api/twitreq", $"{{\"method\": \"POST\",\"url\": \"https://api.twitter.com/oauth/access_token\",\"params\": {{ \"oauth_token\": \"{PINToken}\", \"oauth_verifier\": \"{TwitterPin}\"}}, \"token\": \"{PINToken}\", \"secret\": \"{PINSecret}\"}}");
            platform.Client.SetHeader("Authorization", await resp.GetStringAsync());
            resp = await platform.Client.SendAsync("POST", "https://api.twitter.com/oauth/access_token");
            if (resp.StatusCode != HttpStatusCode.OK)
                throw new UnauthorizedAccessException("Invalid PIN");
            var callbackData = HttpUtility.ParseQueryString(await resp.GetStringAsync());
            OAuthToken = callbackData["oauth_token"];
            OAuthSecret = callbackData["oauth_token_secret"];
            ScreenName = callbackData["screen_name"];
            AuthStatus.Value = $"Logged In ({ScreenName})";
            AuthPin.Visible = false;
            TwitterPin.Visible = false;
            SubmitPin.Visible = false;
            Logout.Visible = true;
            PINSecret = null;
            PINToken = null;
        }

        public void LogoutF(IPlugin import)
        {
            OAuthToken = OAuthSecret = null;
            AuthStatus.Value = "Not Logged In";
            AuthPin.Visible = AuthPin.Enabled = true;
            Logout.Visible = false;
        }

        public void SaveConfig(Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OAuthToken != null);
                if (OAuthToken != null)
                {
                    writer.Write(OAuthToken);
                    writer.Write(OAuthSecret);
                    writer.Write(ScreenName);
                }
            }
        }
        public void LoadConfig(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadBoolean())
                {
                    OAuthToken = reader.ReadString();
                    OAuthSecret = reader.ReadString();
                    if (stream.Position != stream.Length)
                    {
                        ScreenName = reader.ReadString();
                        AuthStatus.Value = $"Logged In ({ScreenName})";
                    }
                    else
                    {
                        AuthStatus.Value = "Logged In";
                    }
                    AuthPin.Visible = false;
                    TwitterPin.Visible = false;
                    SubmitPin.Visible = false;
                    Logout.Visible = true;
                }
            }
        }
    }
}
