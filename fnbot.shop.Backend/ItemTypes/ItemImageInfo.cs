namespace fnbot.shop.Backend.ItemTypes
{
    public class ItemImageInfo : IItemInfo
    {
        public ItemTextInfo Caption { get; }

        public int MinWidth { get; }
        public int MinHeight { get; }

        public int MaxWidth { get; }
        public int MaxHeight { get; }

        public int MaxFileSize { get; }
        public MediaType[] SupportedTypes { get; }

        public ItemImageInfo(ItemTextInfo caption, int minW, int minH, int maxW, int maxH, int fileSize, MediaType[] types)
        {
            Caption = caption;
            MinWidth = minW;
            MinHeight = minH;
            MaxWidth = maxW;
            MaxHeight = maxH;
            MaxFileSize = fileSize;
            SupportedTypes = types;
        }

        public enum MediaType
        {
            JPG,
            PNG,
            GIF,
            WEBP,
            BMP
        }
    }
}
