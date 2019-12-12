using System;
using System.IO;
using SkiaSharp;

namespace fnbot.shop.ItemShop
{
    static class ImageExtensions
    {
        /*public static SKBitmap Resize(this SKImage me, int width, int height)
        {
            var bmp = NewZeroedBitmap(width, height);
            using var pixmap = bmp.PeekPixels();
            me.ScalePixels(pixmap, SKFilterQuality.Medium);
            return bmp;
        }

        public static SKBitmap Resize(this SKBitmap me, int width, int height)
        {
            var bmp = NewZeroedBitmap(width, height);
            using var pixmap = bmp.PeekPixels();
            me.ScalePixels(pixmap, SKFilterQuality.Medium);
            return bmp;
        }*/

        // resizes by resizing to same ratio and cropping out the rest
        public static SKBitmap SmartResize(this SKImage me, int width, int height)
        {
            var origRatio = (float)me.Width / me.Height;
            var newRatio = (float)width / height;
            var bmp = NewZeroedBitmap(width, height);
            using var c = new SKCanvas(bmp);
            switch (origRatio.CompareTo(newRatio))
            {
                case -1:
                    // new is wider
                    // old is taller
                    var newVal = me.Width / newRatio;
                    c.DrawImage(me, SKRect.Create(0, (me.Height - newVal) / 2, me.Width, newVal), SKRect.Create(0, 0, width, height));
                    break;
                case 1:
                    // old is wider
                    // new is taller
                    newVal = me.Height * newRatio;
                    c.DrawImage(me, SKRect.Create((me.Width - newVal) / 2, 0, newVal, me.Height), SKRect.Create(0, 0, width, height));
                    break;
                case 0:
                    // same
                    c.DrawImage(me, SKRect.Create(0, 0, width, height));
                    break;
            }
            return bmp;
        }

        public static void SaveTo(this SKBitmap me, string filename)
        {
            using var fs = File.Create(filename);
            SKImage.FromBitmap(me).Encode().SaveTo(fs);
        }

        public static SKBitmap NewZeroedBitmap(int width, int height) =>
            new SKBitmap(new SKImageInfo(width, height), SKBitmapAllocFlags.ZeroPixels);

        public static float GetHeight(this SKFontMetrics me) => me.Descent - me.Ascent;

        public static SKShader Offset(this SKShader me, float x, float y) =>
            SKShader.CreateLocalMatrix(me, SKMatrix.MakeTranslation(x, y));

        public static SKShader Offset(this SKShader me, SKPoint point) =>
            SKShader.CreateLocalMatrix(me, SKMatrix.MakeTranslation(point.X, point.Y));

        public static SKColor MultiplyColor(SKColor a, SKColor b) =>
            new SKColor((byte)(a.Red * (b.Red / 255f)), (byte)(a.Green * (b.Green / 255f)), (byte)(a.Blue * (b.Blue / 255f)));

        public static MemoryStream AsStream(this ArraySegment<byte> me) => new MemoryStream(me.Array, me.Offset, me.Count, false, true);
    }

    // TODO: Move to another class
    // optimizations for things like Rarity and DrawType enumerations
    public static class EnumHelper<T> where T : Enum
    {
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
    }
}
