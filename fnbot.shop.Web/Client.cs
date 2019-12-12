using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fnbot.shop.Web
{
    // Somewhat based off of https://github.com/discord-net/Discord.Net/blob/dev/src/Discord.Net.Rest/Net/DefaultRestClient.cs for reliablility
    public sealed class Client : IDisposable
    {
        private readonly HttpClient HttpClient;
        public CookieContainer Cookies;

        public Client(bool useCookies = false, bool useProxy = true)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = (DecompressionMethods)3, // GZip and Deflate
                UseCookies = useCookies,
                UseProxy = useProxy
            };
            if (useCookies)
                handler.CookieContainer = Cookies = new CookieContainer();
            HttpClient = new HttpClient(handler);
            SetHeader("accept-encoding", "gzip, deflate");
        }

        public void SetHeader(string key, string value, bool validate = true)
        {
            HttpClient.DefaultRequestHeaders.Remove(key);
            if (value != null)
            {
                if (validate)
                    HttpClient.DefaultRequestHeaders.Add(key, value);
                else
                    HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            }
        }

        public async Task<Response> SendAsync(string method, string uri, bool addBody = true)
        {
            using (var req = new HttpRequestMessage(GetMethod(method), uri))
            {
                var response = await HttpClient.SendAsync(req).ConfigureAwait(false);

                var headers = response.Headers.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
                var stream = addBody ? await response.Content.ReadAsStreamAsync().ConfigureAwait(false) : null;

                return new Response(response.StatusCode, headers, stream, response);
            }
        }
        public async Task<Response> SendJsonAsync(string method, string uri, string json, bool addBody = true)
        {
            using (var req = new HttpRequestMessage(GetMethod(method), uri))
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                req.Content = content;
                return await SendInternalAsync(req, addBody).ConfigureAwait(false);
            }
        }
        public async Task<Response> SendContentAsync(string method, string uri, HttpContent content, bool addBody = true)
        {
            using (var req = new HttpRequestMessage(GetMethod(method), uri))
            using (content)
            {
                req.Content = content;
                return await SendInternalAsync(req, addBody).ConfigureAwait(false);
            }
        }

        public async Task<Response> SendFormAsync(string method, string uri, IReadOnlyDictionary<string, string> formParams, bool addBody = true)
        {
            using (var req = new HttpRequestMessage(GetMethod(method), uri))
            using (var content = new FormUrlEncodedContent(formParams))
            {
                req.Content = content;
                return await SendInternalAsync(req, addBody).ConfigureAwait(false);
            }
        }

        public async Task<Response> SendMultipartAsync(string method, string uri, IReadOnlyDictionary<string, object> multipartParams, bool addBody = true)
        {
            using (var req = new HttpRequestMessage(GetMethod(method), uri))
            using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToBinary()))
            {
                foreach (var p in multipartParams)
                {
                    switch (p.Value)
                    {
                        case string stringValue:
                            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(stringValue)), p.Key);
                            continue;
                        case byte[] byteArrayValue:
                            content.Add(new ByteArrayContent(byteArrayValue), p.Key);
                            continue;
                        case Stream streamValue:
                            content.Add(new StreamContent(streamValue), p.Key);
                            continue;
                        case MultipartFile fileValue:
                            content.Add(new StreamContent(fileValue.Stream), p.Key, fileValue.Filename);
                            continue;
                        case HttpContent contentValue:
                            content.Add(contentValue, p.Key);
                            continue;
                        default:
                            throw new InvalidOperationException($"Unsupported param type \"{p.Value.GetType().Name}\".");
                    }
                }
                req.Content = content;
                return await SendInternalAsync(req, addBody).ConfigureAwait(false);
            }
        }

        private async Task<Response> SendInternalAsync(HttpRequestMessage request, bool addBody = true)
        {
            var response = await HttpClient.SendAsync(request).ConfigureAwait(false);

            var headers = response.Headers.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
            var stream = addBody ? await response.Content.ReadAsStreamAsync().ConfigureAwait(false) : null;

            return new Response(response.StatusCode, headers, stream, response);
        }

        public void Dispose() => HttpClient.Dispose();

        private static HttpMethod GetMethod(string method) =>
            method switch
            {
                "DELETE" => HttpMethod.Delete,
                "GET" => HttpMethod.Get,
                "HEAD" => HttpMethod.Head,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                _ => throw new ArgumentOutOfRangeException(nameof(method), $"Unknown HttpMethod: {method}"),
            };
    }
}
