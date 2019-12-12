using System.IO;

namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemGif : IItem
    {
        public string Caption { get; }
        public int Width { get; }
        public int Height { get; }
        public int Frames { get; }
        public Stream Stream { get; }
        public ItemGifInfo.MediaType Type { get; }

        public ItemGif(string caption, int width, int height, int frames, Stream stream, ItemGifInfo.MediaType type)
        {
            Caption = caption;
            Width = width;
            Height = height;
            Frames = frames;
            Stream = stream;
            Type = type;
        }
    }
}
