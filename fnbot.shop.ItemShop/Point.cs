namespace fnbot.shop.ItemShop
{
    readonly struct Point
    {
        public readonly int X;
        public readonly int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Point((int X, int Y) inp) =>
            new Point(inp.X, inp.Y);
    }
}
