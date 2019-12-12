using System.IO;

namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemImage : IItem
    {
        public string Caption { get; }
        public int Width { get; }
        public int Height { get; }
        public Stream Stream { get; }
        public ItemImageInfo.MediaType Type { get; }

        public ItemImage(string caption, int width, int height, Stream stream, ItemImageInfo.MediaType type)
        {
            Caption = caption;
            Width = width;
            Height = height;
            Stream = stream;
            Type = type;
        }
    }
}
