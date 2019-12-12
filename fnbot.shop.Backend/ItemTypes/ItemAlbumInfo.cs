namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemAlbumInfo : ItemImageInfo
    {
        public int MaxImages { get; }

        public ItemAlbumInfo(ItemTextInfo caption, int minW, int minH, int maxW, int maxH, int fileSize, MediaType[] types, int maxImgs) :
            base(caption, minW, minH, maxW, maxH, fileSize, types)
        {
            MaxImages = maxImgs;
        }
    }
}
