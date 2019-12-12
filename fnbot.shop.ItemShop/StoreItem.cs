using System;
using PakReader.Parsers.Objects;
using SkiaSharp;

namespace fnbot.shop.ItemShop
{
    class StoreItem : IComparable
    {
        public Rarity Rarity = Rarity.UNCOMMON;
        public string Series;
        static readonly int RarityCount = Enum.GetNames(typeof(Rarity)).Length;
        public int RarityInd => string.IsNullOrEmpty(Series) ? (int)Rarity : (ItemShop.Series.RarityTypes.IndexOfKey(Series) + RarityCount);
        public ItemType Type;
        public FText Name;
        public FText Description;
        public FText ShortDescription;
        public StaticPrice Price;
        public string Banner = "";
        public string StandardIcon;
        public string FeaturedIcon;

        public int FeaturedCategory; // starts at 1
        public int SortPriority;

        //public RarityType ColorInfo => string.IsNullOrEmpty(Series) ? Rarities[(int)Rarity] : ShopPoster.Series.GetColorInfo(Series);

        public int CompareTo(object obj) =>
            obj is StoreItem item ?
                item.SortPriority == SortPriority ?
                    item.Banner == Banner ?
                        item.Rarity == Rarity ?
                            Type.CompareTo(item.Type) :
                            item.Rarity.CompareTo(Rarity) :
                    item.Banner.CompareTo(Banner) :
                item.SortPriority.CompareTo(SortPriority) :
            0;

        public override string ToString()
        {
            return $"{FeaturedCategory} {Name.SourceString}";
        }
    }

    sealed class RarityType
    {
        public FText SeriesName;
        public Color SeriesColor;

        public Color GradientColor1;
        public Color GradientColor2;

        public Color TextColor1;
        public Color TextColor2;
    }

    public class StaticPrice
    {
        public readonly int Price;
        public StaticPrice(int price) => Price = price;
    }
    public sealed class DiscountedPrice : StaticPrice
    {
        public readonly int OriginalPrice;
        public DiscountedPrice(int price, int basePrice) : base(price) => OriginalPrice = basePrice;
    }

    public enum Rarity
    {
        COMMON,
        UNCOMMON,
        RARE,
        EPIC,
        LEGENDARY,
        MYTHIC,

        // number of rarities
        COUNT
    }

    public enum ItemType
    {
        UNKNOWN,
        BUNDLE,
        CHARACTER,
        GLIDER,
        PICKAXE,
        PET,
        MUSICPACK,
        BACKPACK,
        DANCE,
        TOKEN,
        ITEMWRAP,
        CONTRAIL,
        EMOJI,
        LOADING,
    }

    readonly struct Color
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public Color(byte v) : this(v, v, v, 255) { }
        public Color(byte r, byte g, byte b) : this(r, g, b, 255) { }
        public Color(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(float v) : this(v, v, v, 255) { }
        public Color(float r, float g, float b) : this(r, g, b, 255) { }
        public Color(float r, float g, float b, float a)
        {
            R = (byte)Math.Round(r * 255);
            G = (byte)Math.Round(g * 255);
            B = (byte)Math.Round(b * 255);
            A = (byte)Math.Round(a * 255);
        }

        public void Deconstruct(out byte r, out byte g, out byte b) => Deconstruct(out r, out g, out b, out _);
        public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        public static Color Lerp(Color a, Color b, float i) => new Color(
                (byte)(a.R + Math.Round(i * (b.R - a.R))),
                (byte)(a.G + Math.Round(i * (b.G - a.G))),
                (byte)(a.B + Math.Round(i * (b.B - a.B))),
                (byte)(a.A + Math.Round(i * (b.A - a.A))));

        public static (float h, float s, float l) RGB2HSL(Color rgb)
        {
            (float h, float s, float l) hsl;
            float r = rgb.R / 255.0f;
            float g = rgb.G / 255.0f;
            float b = rgb.B / 255.0f;
            float min = Math.Min(Math.Min(r, g), b);
            float max = Math.Max(Math.Max(r, g), b);
            float delta = max - min;
            hsl.l = (max + min) / 2;
            if (delta == 0)
            {
                hsl.h = 0;
                hsl.s = 0.0f;
            }
            else
            {
                hsl.s = (hsl.l <= 0.5) ? (delta / (max + min)) : (delta / (2 - max - min));
                float hue;
                if (r == max)
                {
                    hue = ((g - b) / 6) / delta;
                }
                else if (g == max)
                {
                    hue = (1.0f / 3) + ((b - r) / 6) / delta;
                }
                else
                {
                    hue = (2.0f / 3) + ((r - g) / 6) / delta;
                }
                if (hue < 0)
                    hue += 1;
                if (hue > 1)
                    hue -= 1;
                hsl.h = hue;
            }
            return hsl;
        }
        public static Color HSL2RGB(float h, float s, float l)
        {
            h %= 1;
            s %= 1;
            l %= 1;
            if (s == 0)
            {
                return (byte)(l * 255);
            }
            else
            {
                float v1, v2;

                v2 = (l < 0.5) ? (l * (1 + s)) : ((l + s) - (l * s));
                v1 = 2 * l - v2;

                return ((byte)(255 * HueToRGB(v1, v2, h + (1.0f / 3))),
                        (byte)(255 * HueToRGB(v1, v2, h)),
                        (byte)(255 * HueToRGB(v1, v2, h - (1.0f / 3))));
            }

            float HueToRGB(float v1, float v2, float vH)
            {
                if (vH < 0)
                    vH += 1;
                if (vH > 1)
                    vH -= 1;
                if ((6 * vH) < 1)
                    return v1 + (v2 - v1) * 6 * vH;
                if ((2 * vH) < 1)
                    return v2;
                if ((3 * vH) < 2)
                    return v1 + (v2 - v1) * ((2.0f / 3) - vH) * 6;
                return v1;
            }
        }

        public static Color operator +(Color a, Color b) => new Color(
            (byte)Math.Max(a.R + b.R, 255),
            (byte)Math.Max(a.G + b.G, 255),
            (byte)Math.Max(a.B + b.B, 255),
            (byte)Math.Max(a.A + b.A, 255));
        public static Color operator -(Color a, Color b) => new Color(
            (byte)Math.Min(a.R - b.R, 0),
            (byte)Math.Min(a.G - b.G, 0),
            (byte)Math.Min(a.B - b.B, 0),
            (byte)Math.Min(a.A - b.A, 0));

        public static implicit operator Color(byte v) =>
            new Color(v);
        public static implicit operator Color((byte, byte, byte, byte) tuple) =>
            new Color(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        public static implicit operator Color((byte, byte, byte) tuple) =>
            new Color(tuple.Item1, tuple.Item2, tuple.Item3);
        public static explicit operator Color(FLinearColor color) =>
            new Color(color.R, color.G, color.B, color.A);
        public static explicit operator SKColor(Color color) =>
            new SKColor(color.R, color.G, color.B, color.A);

        public override string ToString() => $"({R}, {G}, {B}{(A != 255 ? $", {A}" : "")})";
    }
}
