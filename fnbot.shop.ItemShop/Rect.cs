namespace fnbot.shop.ItemShop
{
    readonly struct Rect
    {
        public readonly int X;
        public readonly int Y;
        public readonly int W;
        public readonly int H;

        public Rect(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public static implicit operator Rect((int X, int Y, int W, int H) inp) =>
            new Rect(inp.X, inp.Y, inp.W, inp.H);
    }
}
