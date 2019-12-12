using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace fnbot.shop.Web
{
    public readonly struct Response : IDisposable
    {
        public HttpStatusCode StatusCode { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public Stream Stream { get; }

        private readonly IDisposable ResponseMsg;

        public Response(HttpStatusCode statusCode, Dictionary<string, string> headers, Stream stream, IDisposable responseMsg)
        {
            StatusCode = statusCode;
            Headers = headers;
            Stream = stream;
            ResponseMsg = responseMsg;
        }

        public Task<string> GetStringAsync()
        {
            using var reader = new StreamReader(Stream);
            return reader.ReadToEndAsync();
        }

        public void Dispose()
        {
            Stream.Dispose();
            ResponseMsg.Dispose();
        }
    }
}
