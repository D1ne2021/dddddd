namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemGifInfo : ItemImageInfo
    {
        public int MaxFrames { get; }

        public new MediaType[] SupportedTypes { get; }

        public ItemGifInfo(ItemTextInfo caption, int minW, int minH, int maxW, int maxH, int fileSize, MediaType[] types, int frames) :
            base(caption, minW, minH, maxW, maxH, fileSize, null)
        {
            MaxFrames = frames;
            SupportedTypes = types;
        }

        public new enum MediaType
        {
            GIF,
            APNG
        }
    }
}
