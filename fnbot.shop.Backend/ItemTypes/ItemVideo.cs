using System.IO;

namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemVideo : IItem
    {
        public string Caption { get; }
        public int Width { get; }
        public int Height { get; }
        public float Duration { get; }
        public float FPS { get; }
        public Stream Stream { get; }
        public ItemVideoInfo.MediaType Type { get; }

        public ItemVideo(string caption, int width, int height, float duration, float fps, Stream stream, ItemVideoInfo.MediaType type)
        {
            Caption = caption;
            Width = width;
            Height = height;
            Stream = stream;
            Duration = duration;
            FPS = fps;
            Type = type;
        }
    }
}
