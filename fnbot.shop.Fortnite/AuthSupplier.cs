using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using fnbot.shop.Web;
using PakReader.Parsers.Objects;

namespace fnbot.shop.Fortnite
{
    public static class AuthSupplier
    {
        const string EPIC_LAUNCHER_AUTHORIZATION = "MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
        const string FORTNITE_AUTHORIZATION = "ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=";

        const string EPIC_OAUTH_TOKEN_ENDPOINT = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
        const string FORTNITE_AES_ENDPOINT = "https://fnbot.shop/api/aes";
        const string FORTNITE_KEYCHAIN_ENDPOINT = "https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/storefront/v2/keychain";
        const string FORTNITE_STOREFRONT_ENDPOINT = "https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/storefront/v2/catalog";

        const ulong CREDS_MAGIC = 0x23FB9C2A_27BD310D;
        const ulong TOKEN_MAGIC = 0xF3641824_772A4C8E;
        const string TOKEN_PREFIX = "eg1~";

        static string CredsPath;
        static string TokenPath;

        static string Username;
        static string Password;

        public static string AccessToken { get; private set; }
        public static string RefreshToken { get; private set; }

        public static long Expires;
        public static bool Expired => Expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        static readonly Client Client = new Client();

        public static async Task Login(string credsPath, string tokenPath)
        {
            CredsPath = credsPath;
            TokenPath = tokenPath;
            if (!File.Exists(CredsPath))
                throw new FileLoadException("No credentials");
            using (var file = File.OpenRead(CredsPath))
            using (var reader = new BinaryReader(file))
            {
                if (reader.ReadUInt64() != CREDS_MAGIC)
                    throw new FileLoadException("Invalid magic");

                // we don't know if the username or password could be the bee movie script or a single letter
                Username = reader.ReadString();
                Password = reader.ReadString();
            }

            if (File.Exists(TokenPath))
            {
                using (var file = File.OpenRead(TokenPath))
                using (var reader = new BinaryReader(file))
                {
                    if (reader.ReadUInt64() != TOKEN_MAGIC)
                        throw new FileLoadException("Invalid magic");

                    var header = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                    AccessToken  = $"{TOKEN_PREFIX}{header}.{Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()))}";
                    RefreshToken = $"{TOKEN_PREFIX}{header}.{Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()))}";
                    Expires = JsonSerializer.Deserialize<AccessTokenData>(FromBase64String(AccessToken.Split('.')[1])).exp;
                }
                await RefreshIfInvalidAsync();
            }
            else
            {
                await LoginInternalAsync();
                await WriteTokensAsync();
            }

            Client.SetHeader("Authorization", "bearer " + AccessToken);
        }

        static async Task LoginInternalAsync()
        {
            Console.WriteLine("Logging in");
            using (var client = new Client(true))
            {
                await client.SendAsync("HEAD", "https://www.epicgames.com/id/api/csrf", false);
                client.SetHeader("x-xsrf-token", client.Cookies.GetCookies(new Uri("https://www.epicgames.com/id/api/csrf"))["XSRF-TOKEN"].Value);

                {
                    var rs = await client.SendFormAsync("POST", "https://www.epicgames.com/id/api/login", new Dictionary<string, string>
                    {
                        { "email", Username },
                        { "password", Password },
                        { "rememberMe", "true" }
                    }, false).ConfigureAwait(false);
                    if (rs.StatusCode == HttpStatusCode.Conflict)
                    {
                        await client.SendFormAsync("POST", "https://www.epicgames.com/id/api/login", new Dictionary<string, string>
                        {
                            { "email", Username },
                            { "password", Password },
                            { "rememberMe", "true" }
                        }, false).ConfigureAwait(false);
                    }
                }

                var exchange_resp = await JsonSerializer.DeserializeAsync<ExchangeResp>((await client.SendAsync("GET", "https://www.epicgames.com/id/api/exchange")).Stream);

                client.SetHeader("Authorization", "basic " + EPIC_LAUNCHER_AUTHORIZATION);
                var r = (await client.SendFormAsync("POST", EPIC_OAUTH_TOKEN_ENDPOINT, new Dictionary<string, string>
                {
                    { "grant_type", "exchange_code" },
                    { "exchange_code", exchange_resp.code },
                    { "token_type", "eg1" }
                }));
                var resp = await JsonSerializer.DeserializeAsync<AuthResp>(r.Stream);
                AccessToken = resp.access_token;
                RefreshToken = resp.refresh_token;
                Expires = JsonSerializer.Deserialize<AccessTokenData>(FromBase64String(AccessToken.Split('.')[1])).exp;
            }
        }

        static async Task RefreshIfInvalidAsync()
        {
            if (Expired)
            {
                Console.WriteLine("Refreshing");
                Client.SetHeader("Authorization", FORTNITE_AUTHORIZATION, false);
                var resp = await JsonSerializer.DeserializeAsync<AuthResp>((await Client.SendFormAsync("POST", EPIC_OAUTH_TOKEN_ENDPOINT, new Dictionary<string, string>()
                {
                    {"grant_type", "refresh_token" },
                    {"refresh_token", RefreshToken},
                    {"token_type", "eg1" }
                })).Stream);
                if (resp.access_token == null) // refresh token is invalid, etc.
                {
                    await LoginInternalAsync();
                }
                else
                {
                    AccessToken = resp.access_token;
                    RefreshToken = resp.refresh_token;
                }
                await WriteTokensAsync();
                Client.SetHeader("Authorization", "bearer " + AccessToken);
            }
        }

        static async Task WriteTokensAsync()
        {
            using (var file = File.Open(TokenPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(file))
            {
                writer.Write(TOKEN_MAGIC);

                var splits = AccessToken.Substring(TOKEN_PREFIX.Length).Split('.', 2);

                var buffer = Encoding.UTF8.GetBytes(splits[0]); // header
                writer.Write((byte)buffer.Length);
                await file.WriteAsync(buffer, 0, buffer.Length);

                buffer = Encoding.UTF8.GetBytes(splits[1]); // access body and key
                writer.Write((ushort)buffer.Length);
                await file.WriteAsync(buffer, 0, buffer.Length);

                buffer = Encoding.UTF8.GetBytes(RefreshToken.Substring(TOKEN_PREFIX.Length).Split('.', 2)[1]); // refresh body and key
                writer.Write((ushort)buffer.Length);
                await file.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public static void WriteCreds(string path, string user, string pass)
        {
            using (var file = File.Open(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(file))
            {
                writer.Write(CREDS_MAGIC);
                writer.Write(user);
                writer.Write(pass);
            }
        }

        public static async Task<Dictionary<FGuid, byte[]>> GetKeychainAsync()
        {
            await RefreshIfInvalidAsync();
            var resp = await JsonSerializer.DeserializeAsync<string[]>((await Client.SendAsync("GET", FORTNITE_KEYCHAIN_ENDPOINT)).Stream);
            var dict = new Dictionary<FGuid, byte[]>(resp.Length);
            for (int i = 0; i < resp.Length; i++)
            {
                var splits = resp[i].Split(':');
                dict[new FGuid(splits[0])] = Convert.FromBase64String(splits[1]);
            }
            return dict;
        }

        static JsonSerializerOptions StorefrontSerializer;
        public static async Task<Storefront> GetStorefrontAsync()
        {
            if (StorefrontSerializer == null)
            {
                StorefrontSerializer = new JsonSerializerOptions();
                StorefrontSerializer.Converters.Add(new JsonStringEnumConverter());
            }
            await RefreshIfInvalidAsync();
            return await JsonSerializer.DeserializeAsync<Storefront>((await Client.SendAsync("GET", FORTNITE_STOREFRONT_ENDPOINT)).Stream, StorefrontSerializer);
        }

        public static async Task<byte[]> GetKeyAsync() =>
            StringToByteArray(await (await Client.SendAsync("GET", FORTNITE_AES_ENDPOINT)).GetStringAsync());

        static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
            }

            return arr;
        }
        static int GetHexVal(char hex) =>
            hex - (hex < 58 ? 48 : 87);

        // Add padding if necessary
        static string FromBase64String(string inp)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(inp));
                }
                catch (FormatException) { }
                inp += "=";
            }
            throw new ArgumentException("Invalid B64", nameof(inp));
        }

        struct ExchangeResp
        {
            public string code { get; set; }
            public string errorMessage { get; set; }
        }

        struct AuthResp
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
        }

        struct AccessTokenData
        {
            public long exp { get; set; }
        }
    }
}
