namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemVideoInfo : ItemImageInfo
    {
        public float MinDuration { get; }
        public float MaxDuration { get; }

        public float MinFPS { get; }
        public float MaxFPS { get; }

        public new MediaType[] SupportedTypes { get; }

        public ItemVideoInfo(ItemTextInfo caption, int minW, int minH, int maxW, int maxH, int fileSize, float minFPS, float maxFPS, MediaType[] types) :
            base(caption, minW, minH, maxW, maxH, fileSize, null)
        {
            MinFPS = minFPS;
            MaxFPS = maxFPS;
            SupportedTypes = types;
        }

        public new enum MediaType
        {
            MP4,
            MOV,
            AVI,
            WEBM
        }
    }
}
