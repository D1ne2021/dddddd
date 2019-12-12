using System.IO;

namespace fnbot.shop.Web
{
    public readonly struct MultipartFile
    {
        public Stream Stream { get; }
        public string Filename { get; }

        public MultipartFile(Stream stream, string filename)
        {
            Stream = stream;
            Filename = filename;
        }
    }
}
