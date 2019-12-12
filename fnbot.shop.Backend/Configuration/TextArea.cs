namespace fnbot.shop.Backend.Configuration
{
    public readonly struct TextArea
    {
        public string Value { get; }
        public TextArea(string value)
        {
            Value = value;
        }

        public static implicit operator TextArea(string s) => new TextArea(s);

        public override string ToString() => Value;
    }
}
