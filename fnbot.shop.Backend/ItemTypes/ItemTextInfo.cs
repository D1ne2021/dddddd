namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemTextInfo : IItemInfo
    {
        public int MaxLength { get; }
        public char[] AcceptableCharacters { get; }

        public ItemTextInfo(int length, char[] characters)
        {
            MaxLength = length;
            AcceptableCharacters = characters;
        }
    }
}
