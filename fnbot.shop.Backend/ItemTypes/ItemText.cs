namespace fnbot.shop.Backend.ItemTypes
{
    public sealed class ItemText : IItem
    {
        public string Text { get; }

        public ItemText(string text)
        {
            Text = text;
        }
    }
}
