using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace fnbot.shop.Web
{
    public sealed class Content : HttpContent
    {
        readonly HttpContent content;
        readonly MemoryStream stream;

        public Content(HttpContent content)
        {
            stream = new MemoryStream();
            this.content = content;
            content.CopyToAsync(stream).GetAwaiter().GetResult();
            foreach (var kv in content.Headers)
            {
                Headers.Add(kv.Key, kv.Value);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
            this.stream.CopyToAsync(stream);

        protected override bool TryComputeLength(out long length)
        {
            length = stream.Length;
            return true;
        }
    }
}
