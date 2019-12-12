namespace fnbot.shop.Backend.Configuration
{
    public readonly struct StringLabel
    {
        public string Value { get; }
        public StringLabel(string value)
        {
            Value = value;
        }

        public static implicit operator StringLabel(string s) => new StringLabel(s);

        public override string ToString() => Value;
    }
}
