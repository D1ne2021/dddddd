using System.IO;

namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemAlbum : IItem
    {
        public string Caption { get; }
        public int Width { get; }
        public int Height { get; }
        public Stream[] Streams { get; }
        public ItemImageInfo.MediaType Type { get; }

        public ItemAlbum(string caption, int width, int height, Stream[] streams, ItemImageInfo.MediaType type)
        {
            Caption = caption;
            Width = width;
            Height = height;
            Streams = streams;
            Type = type;
        }
    }
}
