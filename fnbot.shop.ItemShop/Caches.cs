using System;
using System.Collections.Generic;
using fnbot.shop.Fortnite;
using PakReader.Pak;
using PakReader.Parsers;
using SkiaSharp;

namespace fnbot.shop.ItemShop
{
    // Persistent across shops, category size independent
    class GlobalCache
    {
        public PakIndex Index;

        ColorPalette[] RarityColors;
        SortedList<string, ColorPalette> SeriesColors;

        public SKImage VBucksIcon;
        public SKShader BaseBorderShader;
        Dictionary<string, SKBitmap> AdspaceArrows = new Dictionary<string, SKBitmap>();

        public SKTypeface NameTypeface;
        public SKTypeface ShortDescriptionTypeface;
        public SKTypeface PriceTypeface;
        public SKTypeface CategoryTypeface;

        public SKPaint ImagePaint;

        public int CategoryCount;
        SectionCache[] SectionCaches;

        public GlobalCache(PakIndex index)
        {
            Index = index;
            VBucksIcon = index.GetPackage("/FortniteGame/Content/UI/Foundation/Shell/Textures/T-Icon-VBucks-L").GetExport<Texture2D>().Image;

            var img = index.GetPackage("/FortniteGame/Content/VisualThreatManager/StormVisuals/Test/SpawnParticles/Streamers/LowResBlurredNoise").GetExport<Texture2D>().Image; // don't dispose objects given by exports
            using (var b = SKBitmap.FromImage(img))
            using (var b2 = new SKBitmap(new SKImageInfo(b.Width * 2, b.Height * 2), SKBitmapAllocFlags.ZeroPixels))
            {
                using (var c = new SKCanvas(b2))
                using (var s = SKShader.CreateColorFilter(SKShader.CreateBitmap(b, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat), SKColorFilter.CreateLighting(new SKColor(160, 160, 160), new SKColor(15, 15, 15))))
                {
                    c.DrawRect(0, 0, b2.Width, b2.Height, new SKPaint { Shader = s });
                }
                using (var borderNoiseBig = b2.Resize(new SKImageInfo(b2.Width * 16, b2.Height * 16), SKFilterQuality.Medium))
                using (var borderNoise = new SKBitmap(b.Width * 16, b.Width * 16))
                {
                    borderNoiseBig.ExtractSubset(borderNoise, new SKRectI(b2.Width * 4, b2.Width * 4, b2.Width * 12, b2.Width * 12));
                    BaseBorderShader = SKShader.CreateBitmap(borderNoise, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
                }
            }

            NameTypeface = SKTypeface.FromStream(index.GetFile("/FortniteGame/Content/UI/Foundation/Fonts/BurbankBigCondensed-Black.ufont").AsStream());
            ShortDescriptionTypeface = SKTypeface.FromStream(index.GetFile("/FortniteGame/Content/UI/Foundation/Fonts/NotoSans-Regular.ufont").AsStream());
            PriceTypeface = SKTypeface.FromStream(index.GetFile("/FortniteGame/Content/UI/Foundation/Fonts/NotoSans-Bold.ufont").AsStream());
            CategoryTypeface = SKTypeface.FromStream(index.GetFile("/FortniteGame/Content/UI/Foundation/Fonts/NotoSans-Bold.ufont").AsStream());

            ImagePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };

            {
                var types = EnumHelper<DrawType>.Values;
                SectionCaches = new SectionCache[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    SectionCaches[i] = new SectionCache(types[i], this);
                }
            }

            {
                RarityColors = new ColorPalette[]
                {
                new ColorPalette(this, default, new SKColor(150, 150, 150), new SKColor(50, 53, 58), new SKColor(212, 212, 212), new SKColor(249, 249, 249)), // COMMON
                new ColorPalette(this, default, new SKColor(100, 165, 60), new SKColor(18, 70, 20), new SKColor(241, 255, 212), new SKColor(249, 255, 240)), // UNCOMMON
                new ColorPalette(this, default, new SKColor(55, 160, 240), new SKColor(18, 55, 120), new SKColor(212, 255, 255), new SKColor(250, 254, 255)), // RARE
                new ColorPalette(this, default, new SKColor(180, 100, 220), new SKColor(83, 45, 130), new SKColor(252, 220, 255), new SKColor(252, 215, 255)), // EPIC
                new ColorPalette(this, default, new SKColor(220, 135, 80), new SKColor(120, 60, 40), new SKColor(255, 255, 219), new SKColor(255, 255, 245)), // LEGENDARY
                new ColorPalette(this, default, new SKColor(215, 188, 102), new SKColor(117, 96, 42), new SKColor(255, 254, 255), new SKColor(255, 254, 232)), // MYTHIC
                };
                SeriesColors = new SortedList<string, ColorPalette>();
            }
        }

        public SectionCache GetSection(DrawType type) =>
            SectionCaches[(int)type];

        public ColorPalette GetPalette(Rarity rarity) =>
            RarityColors[(int)rarity];

        public ColorPalette GetPalette(string series)
        {
            if (SeriesColors.TryGetValue(series, out var ret))
            {
                return ret;
            }
            var type = Series.RarityTypes[series];
            return SeriesColors[series] = new ColorPalette(this, (SKColor)type.SeriesColor, (SKColor)type.GradientColor1, (SKColor)type.GradientColor2, (SKColor)type.TextColor1, (SKColor)type.TextColor2);
        }

        public ColorPalette GetPalette(StoreItem item) =>
            item.Series == null ? GetPalette(item.Rarity) : GetPalette(item.Series);
    }

    // One for featured and daily
    class SectionCache
    {
        public GlobalCache Global { get; }
        public DrawType Type { get; }

        public int HEIGHT { get; }
        public int WIDTH { get; }

        // height of just the price box
        public int PRICE_HEIGHT { get; }
        // height of just the description (gray box)
        public int DESCRIPTION_HEIGHT { get; }
        // distance from the top of the gray box to the item name text
        public int NAME_TOP_MARGIN { get; }
        // distance from the top of the gray box to the short description
        public int SHORT_DESCRIPTION_TOP_MARGIN { get; }
        // space between the vbucks icon and the price (or the price and the crossed out original price)
        public int PRICE_SIDE_PADDING { get; }
        // space between the discounted text and the red cross
        public float DISCOUNTED_PRICE_LINE_OFFSET_X { get; }

        public float NAME_TEXT_SIZE { get; }
        public float SHORT_DESCRIPTION_TEXT_SIZE { get; }
        public float PRICE_TEXT_SIZE { get; }

        // center of the background gradient
        public SKPoint BACKGROUND_GRADIENT_CENTER { get; }
        // radius of the background gradient (scale this based on the width or height: the ratios stay visibly constant so it doesn't matter)
        public int BACKGROUND_GRADIENT_RADIUS { get; }
        // gradient points of the name's color
        public float[] NAME_GRADIENT_POINTS { get; }

        public float CATEGORY_BOX_SIDE_PADDING { get; }
        public float CATEGORY_BOX_BORDER_WIDTH { get; }
        public float CATEGORY_BOX_PROGRESS_HEIGHT { get; }
        public float CATEGORY_BOX_TEXT_SIZE { get; }
        public float CATEGORY_BOX_WIDTH { get; }
        public float CATEGORY_BOX_HEIGHT { get; }

        Dictionary<int, CachedBitmap> Prices = new Dictionary<int, CachedBitmap>();
        Dictionary<string, CachedBitmap> SeriesBgs = new Dictionary<string, CachedBitmap>();
        Dictionary<(int A, int B), SKBitmap> CategoryText; // only for featured because dailies dont have categories

        Dictionary<StoreItem, ItemCache> ItemCache = new Dictionary<StoreItem, ItemCache>();

        public SKPaint NamePaint;
        public SKPaint NameStrokePaint;
        public SKPaint ShortDescriptionPaint;
        public SKPaint PricePaint;
        public SKPaint DiscountedPricePaint;
        public SKPaint CategoryPaint;

        public SectionCache(DrawType type, GlobalCache global)
        {
            Global = global;
            Type = type;
            switch (type)
            {
                case DrawType.FEATURED:
                    HEIGHT = Config.TOTAL_HEIGHT;
                    WIDTH = Config.FEATURED_WIDTH;

                    DESCRIPTION_HEIGHT = 86; // fix this shit lmao
                    NAME_TOP_MARGIN = 4;
                    SHORT_DESCRIPTION_TOP_MARGIN = 52;
                    PRICE_HEIGHT = 56;

                    BACKGROUND_GRADIENT_CENTER = new SKPoint(.6f, .375f);
                    BACKGROUND_GRADIENT_RADIUS = 347;
                    const float f = 0.4935f;
                    NAME_GRADIENT_POINTS = new float[]
                    {
                    f-.0005f,
                    f,
                    f,
                    f+.006f,
                    f+.006f+.003f
                    };
                    PRICE_SIDE_PADDING = 7;
                    DISCOUNTED_PRICE_LINE_OFFSET_X = 4;

                    NAME_TEXT_SIZE = 37;
                    SHORT_DESCRIPTION_TEXT_SIZE = 20;
                    PRICE_TEXT_SIZE = 28;

                    CATEGORY_BOX_BORDER_WIDTH = 3;
                    CATEGORY_BOX_SIDE_PADDING = 7;
                    CATEGORY_BOX_WIDTH = 84;
                    CATEGORY_BOX_HEIGHT = 40;
                    CATEGORY_BOX_TEXT_SIZE = 20;
                    CATEGORY_BOX_PROGRESS_HEIGHT = 4;
                    break;
                case DrawType.DAILY:
                    HEIGHT = (int)Math.Round((Config.TOTAL_HEIGHT - Config.ITEM_PADDING) / 2f);
                    WIDTH = Config.DAILY_WIDTH;
                    DESCRIPTION_HEIGHT = 66; // fix this shit lmao
                    NAME_TOP_MARGIN = 1;
                    SHORT_DESCRIPTION_TOP_MARGIN = 34;
                    PRICE_HEIGHT = 36;

                    BACKGROUND_GRADIENT_CENTER = new SKPoint(.605f, .37f);
                    BACKGROUND_GRADIENT_RADIUS = 230;
                    const float d = 0.4965f;
                    NAME_GRADIENT_POINTS = new float[]
                    {
                    d-(.0005f*(28/37f)),
                    d,
                    d,
                    d+(.006f*(28/37f)),
                    d+((.006f+.003f)*(28/37f))
                    };
                    PRICE_SIDE_PADDING = 7;
                    DISCOUNTED_PRICE_LINE_OFFSET_X = 4;

                    NAME_TEXT_SIZE = 28;
                    SHORT_DESCRIPTION_TEXT_SIZE = 20;
                    PRICE_TEXT_SIZE = 20;
                    break;
                default:
                    return;
            }
            NamePaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = Global.NameTypeface,
                TextSize = NAME_TEXT_SIZE // is 28 for daily, but calculate this instead of having a constant
            };
            NameStrokePaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = Global.NameTypeface,
                TextSize = NAME_TEXT_SIZE, // 28 daily
                Style = SKPaintStyle.StrokeAndFill,
                Color = new SKColor(0, 0, 0, 30),
                StrokeWidth = 5,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            ShortDescriptionPaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = Global.ShortDescriptionTypeface,
                TextSize = SHORT_DESCRIPTION_TEXT_SIZE, // 18 daily
                Color = new SKColor(200, 200, 200)
            };
            PricePaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = Global.PriceTypeface,
                TextSize = PRICE_TEXT_SIZE, // 17 daily
                Color = SKColors.White
            };
            DiscountedPricePaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = Global.PriceTypeface,
                TextSize = PRICE_TEXT_SIZE * 0.75f, // 17 daily
                Color = new SKColor(170, 170, 170)
            };
            CategoryPaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = Global.CategoryTypeface,
                TextSize = CATEGORY_BOX_TEXT_SIZE,
                Color = new SKColor(200, 200, 200, 230),
                TextAlign = SKTextAlign.Center
            };
        }

        public ItemCache GetItem(StoreItem item)
        {
            if (!ItemCache.TryGetValue(item, out var cache))
            {
                cache = ItemCache[item] = new ItemCache(Global, item, this);
            }
            return cache;
        }

        public SKBitmap GetPrice(StaticPrice price) // TODO: Cache the bitmap
        {
            string priceTxt = price.Price.ToString("N0");
            float priceWidth;
            float width = priceWidth = PricePaint.MeasureText(priceTxt);
            if (price is DiscountedPrice)
            {
                string origPriceTxt = ((DiscountedPrice)price).OriginalPrice.ToString("N0");
                width += PRICE_SIDE_PADDING + DiscountedPricePaint.MeasureText(origPriceTxt) + DISCOUNTED_PRICE_LINE_OFFSET_X * 2;
            }
            var bmp = ImageExtensions.NewZeroedBitmap((int)width, (int)PricePaint.FontMetrics.GetHeight());
            using (var c = new SKCanvas(bmp))
            {
                c.DrawText(priceTxt, 0, -PricePaint.FontMetrics.Ascent, PricePaint);
                if (price is DiscountedPrice)
                {
                    string origPriceTxt = ((DiscountedPrice)price).OriginalPrice.ToString("N0");
                    c.DrawText(origPriceTxt, priceWidth + PRICE_SIDE_PADDING + DISCOUNTED_PRICE_LINE_OFFSET_X, (-PricePaint.FontMetrics.Ascent + -DiscountedPricePaint.FontMetrics.Ascent) / 2f, DiscountedPricePaint);
                    c.DrawLine(priceWidth + PRICE_SIDE_PADDING, (-PricePaint.FontMetrics.Ascent + -DiscountedPricePaint.FontMetrics.Ascent) / 2f,
                        width, (-PricePaint.FontMetrics.Ascent - DiscountedPricePaint.FontMetrics.Ascent) / 2f + DiscountedPricePaint.FontMetrics.Ascent * .75f, // don't ask about the .75f
                        new SKPaint { IsAntialias = true, Color = new SKColor(225, 86, 75), StrokeWidth = 3f, StrokeCap = SKStrokeCap.Butt });
                }
            }
            return bmp;
        }

        public void Draw(SKCanvas canvas, StoreItem[] items)
        {
            switch (Type)
            {
                case DrawType.FEATURED:
                    {
                        var xVal = Config.SIDE_PADDING;
                        var yVal = (Config.SCREEN_HEIGHT - Config.TOTAL_HEIGHT) / 2f;
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (items[i].Series != null)
                            {
                                canvas.DrawBitmap(Global.GetPalette(items[i]).GetBackground(Type), xVal, yVal);
                            }
                            canvas.DrawBitmap(Global.GetPalette(items[i]).GetBackground(Type), xVal, yVal);
                            canvas.DrawBitmap(GetItem(items[i]).Icon, new SKRect(Config.BORDER_WIDTH, Config.BORDER_WIDTH, WIDTH - Config.BORDER_WIDTH, HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH), SKRect.Create(xVal + Config.BORDER_WIDTH, yVal + Config.BORDER_WIDTH, WIDTH - Config.BORDER_WIDTH * 2, HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH * 2), Global.ImagePaint);
                            canvas.DrawBitmap(GetItem(items[i]).BlurredIcon, xVal + Config.BORDER_WIDTH, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH);

                            canvas.DrawRect(SKRect.Create(xVal + Config.BORDER_WIDTH, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH, WIDTH - Config.BORDER_WIDTH * 2, DESCRIPTION_HEIGHT), new SKPaint { Color = Global.GetPalette(items[i]).DescriptionBox });
                            canvas.DrawBitmap(GetItem(items[i]).Name, xVal + (WIDTH - GetItem(items[i]).Name.Width) / 2f, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH + NAME_TOP_MARGIN);
                            canvas.DrawBitmap(GetItem(items[i]).ShortDescription, xVal + (WIDTH - GetItem(items[i]).ShortDescription.Width) / 2f, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH + SHORT_DESCRIPTION_TOP_MARGIN);

                            canvas.DrawRect(SKRect.Create(xVal + Config.BORDER_WIDTH, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT, WIDTH - Config.BORDER_WIDTH * 2, PRICE_HEIGHT), new SKPaint { Color = new SKColor(0, 7, 36) });
                            {
                                var priceBmp = GetPrice(items[i].Price);
                                var totalWidth = 34 + PRICE_SIDE_PADDING + priceBmp.Width;
                                canvas.DrawImage(Global.VBucksIcon, SKRect.Create(xVal + (WIDTH - totalWidth) / 2f, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT + (PRICE_HEIGHT - 34) / 2f, 34, 34), Global.ImagePaint);
                                canvas.DrawBitmap(priceBmp, xVal + (WIDTH - totalWidth) / 2f + 34 + PRICE_SIDE_PADDING, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT + (PRICE_HEIGHT - priceBmp.Height) / 2f);
                            }
                            xVal += WIDTH + Config.ITEM_PADDING;
                        }
                    }
                    break;
                case DrawType.DAILY:
                    {
                        var xVal = Config.SIDE_PADDING + Config.SelectedCategory * Global.GetSection(DrawType.FEATURED).WIDTH + Config.ITEM_PADDING * (Config.SelectedCategory - 1) + Config.SECTION_PADDING;
                        var yVal = (Config.SCREEN_HEIGHT - Config.TOTAL_HEIGHT) / 2f;
                        for (int i = 0; i < items.Length; i++)
                        {
                            canvas.DrawBitmap(Global.GetPalette(items[i]).GetBackground(Type), xVal, yVal);
                            canvas.DrawBitmap(GetItem(items[i]).Icon, new SKRect(Config.BORDER_WIDTH, Config.BORDER_WIDTH, WIDTH - Config.BORDER_WIDTH, HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH), SKRect.Create(xVal + Config.BORDER_WIDTH, yVal + Config.BORDER_WIDTH, WIDTH - Config.BORDER_WIDTH * 2, HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH * 2), Global.ImagePaint);
                            canvas.DrawBitmap(GetItem(items[i]).BlurredIcon, xVal + Config.BORDER_WIDTH, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH);

                            canvas.DrawRect(SKRect.Create(xVal + Config.BORDER_WIDTH, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH, WIDTH - Config.BORDER_WIDTH * 2, DESCRIPTION_HEIGHT), new SKPaint { Color = Global.GetPalette(items[i]).DescriptionBox });
                            canvas.DrawBitmap(GetItem(items[i]).Name, xVal + (WIDTH - GetItem(items[i]).Name.Width) / 2f, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH + NAME_TOP_MARGIN);
                            canvas.DrawBitmap(GetItem(items[i]).ShortDescription, xVal + (WIDTH - GetItem(items[i]).ShortDescription.Width) / 2f, yVal + HEIGHT - PRICE_HEIGHT - DESCRIPTION_HEIGHT - Config.BORDER_WIDTH + SHORT_DESCRIPTION_TOP_MARGIN);

                            canvas.DrawRect(SKRect.Create(xVal + Config.BORDER_WIDTH, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT, WIDTH - Config.BORDER_WIDTH * 2, PRICE_HEIGHT), new SKPaint { Color = new SKColor(0, 7, 36) });
                            {
                                var priceBmp = GetPrice(items[i].Price);
                                var totalWidth = 34 + PRICE_SIDE_PADDING + priceBmp.Width;
                                canvas.DrawImage(Global.VBucksIcon, SKRect.Create(xVal + (WIDTH - totalWidth) / 2f, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT + (PRICE_HEIGHT - 34) / 2f, 34, 34), Global.ImagePaint);
                                canvas.DrawBitmap(priceBmp, xVal + (WIDTH - totalWidth) / 2f + 34 + PRICE_SIDE_PADDING, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT + (PRICE_HEIGHT - priceBmp.Height) / 2f);
                            }
                            if (i == 2)
                            {
                                xVal -= (WIDTH + Config.ITEM_PADDING) * 2;
                                yVal += HEIGHT + Config.ITEM_PADDING;
                            }
                            else
                            {
                                xVal += WIDTH + Config.ITEM_PADDING;
                            }
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException("Type is invalid");
            }
        }

        public void DrawBorder(SKCanvas canvas, StoreItem[] items, float offset)
        {
            switch (Type)
            {
                case DrawType.FEATURED:
                    {
                        var xVal = Config.SIDE_PADDING;
                        var yVal = (Config.SCREEN_HEIGHT - Config.TOTAL_HEIGHT) / 2f;
                        for (int i = 0; i < items.Length; i++)
                        {
                            var paint = new SKPaint { Shader = Global.GetPalette(items[i]).GetBorder(xVal, yVal, offset) };
                            canvas.DrawRect(xVal, yVal, WIDTH, Config.BORDER_WIDTH, paint);
                            canvas.DrawRect(xVal, yVal + HEIGHT - Config.BORDER_WIDTH, WIDTH, Config.BORDER_WIDTH, paint);
                            canvas.DrawRect(xVal, yVal + Config.BORDER_WIDTH, Config.BORDER_WIDTH, HEIGHT - Config.BORDER_WIDTH * 2, paint);
                            canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH, yVal + Config.BORDER_WIDTH, Config.BORDER_WIDTH, HEIGHT - Config.BORDER_WIDTH * 2, paint);
                            xVal += WIDTH + Config.ITEM_PADDING;
                        }
                    }
                    break;
                case DrawType.DAILY:
                    {
                        var xVal = Config.SIDE_PADDING + Config.SelectedCategory * Global.GetSection(DrawType.FEATURED).WIDTH + Config.ITEM_PADDING * (Config.SelectedCategory - 1) + Config.SECTION_PADDING;
                        var yVal = (Config.SCREEN_HEIGHT - Config.TOTAL_HEIGHT) / 2f;
                        for (int i = 0; i < items.Length; i++)
                        {
                            var paint = new SKPaint { Shader = Global.GetPalette(items[i]).GetBorder(xVal, yVal, offset) };
                            canvas.DrawRect(xVal, yVal, WIDTH, Config.BORDER_WIDTH, paint);
                            canvas.DrawRect(xVal, yVal + HEIGHT - Config.BORDER_WIDTH, WIDTH, Config.BORDER_WIDTH, paint);
                            canvas.DrawRect(xVal, yVal + Config.BORDER_WIDTH, Config.BORDER_WIDTH, HEIGHT - Config.BORDER_WIDTH * 2, paint);
                            canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH, yVal + Config.BORDER_WIDTH, Config.BORDER_WIDTH, HEIGHT - Config.BORDER_WIDTH * 2, paint);
                            if (i == 2)
                            {
                                xVal -= (WIDTH + Config.ITEM_PADDING) * 2;
                                yVal += HEIGHT + Config.ITEM_PADDING;
                            }
                            else
                            {
                                xVal += WIDTH + Config.ITEM_PADDING;
                            }
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException("Type is invalid");
            }
        }

        public void InitializeCategory(SKCanvas canvas, StoreItem[] items, int[] sizes, int category, float amount)
        {
            if (Type != DrawType.FEATURED)
                throw new InvalidOperationException("Type needs to be featured");

            var xVal = Config.SIDE_PADDING;
            var yVal = (Config.SCREEN_HEIGHT - Config.TOTAL_HEIGHT) / 2f;
            for (int i = 0; i < items.Length; i++)
            {
                if (sizes[i] != 1)
                {
                    canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_WIDTH, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT - DESCRIPTION_HEIGHT - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_HEIGHT, CATEGORY_BOX_WIDTH, CATEGORY_BOX_HEIGHT, new SKPaint { Color = new SKColor(255, 255, 255, 50) });
                    canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_WIDTH + CATEGORY_BOX_BORDER_WIDTH, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT - DESCRIPTION_HEIGHT - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_HEIGHT + CATEGORY_BOX_BORDER_WIDTH, CATEGORY_BOX_WIDTH - CATEGORY_BOX_BORDER_WIDTH * 2, CATEGORY_BOX_HEIGHT - CATEGORY_BOX_BORDER_WIDTH * 2, new SKPaint { Color = new SKColor(0, 0, 0, 127) });
                    canvas.DrawText($"{category} of {sizes[i]}", xVal + WIDTH - Config.BORDER_WIDTH - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_WIDTH / 2f, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT - DESCRIPTION_HEIGHT - CATEGORY_BOX_SIDE_PADDING - (CATEGORY_BOX_HEIGHT - CATEGORY_BOX_PROGRESS_HEIGHT - CategoryPaint.FontMetrics.Descent) / 2f, CategoryPaint);

                    canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_WIDTH + CATEGORY_BOX_BORDER_WIDTH, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT - DESCRIPTION_HEIGHT - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_BORDER_WIDTH - CATEGORY_BOX_PROGRESS_HEIGHT, CATEGORY_BOX_WIDTH - CATEGORY_BOX_BORDER_WIDTH * 2, CATEGORY_BOX_PROGRESS_HEIGHT, new SKPaint { Color = new SKColor(0, 0, 0, 50) });
                    if (amount != 0)
                    {
                        canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_WIDTH + CATEGORY_BOX_BORDER_WIDTH, yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT - DESCRIPTION_HEIGHT - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_BORDER_WIDTH - CATEGORY_BOX_PROGRESS_HEIGHT, (CATEGORY_BOX_WIDTH - CATEGORY_BOX_BORDER_WIDTH * 2) * amount, CATEGORY_BOX_PROGRESS_HEIGHT, new SKPaint { Color = new SKColor(255, 255, 255, 180) });
                    }
                }
                xVal += WIDTH + Config.ITEM_PADDING;
            }
        }

        public void TickCategory(SKCanvas canvas, StoreItem[] items, int[] sizes, float prevAmt, float amount)
        {
            if (Type != DrawType.FEATURED)
                throw new InvalidOperationException("Type needs to be featured");

            var xVal = Config.SIDE_PADDING;
            var yVal = (Config.SCREEN_HEIGHT - Config.TOTAL_HEIGHT) / 2f;
            for (int i = 0; i < items.Length; i++)
            {
                if (sizes[i] != 1)
                    canvas.DrawRect(xVal + WIDTH - Config.BORDER_WIDTH - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_WIDTH + CATEGORY_BOX_BORDER_WIDTH + (CATEGORY_BOX_WIDTH - CATEGORY_BOX_BORDER_WIDTH * 2) * (prevAmt), yVal + HEIGHT - Config.BORDER_WIDTH - PRICE_HEIGHT - DESCRIPTION_HEIGHT - CATEGORY_BOX_SIDE_PADDING - CATEGORY_BOX_BORDER_WIDTH - CATEGORY_BOX_PROGRESS_HEIGHT, (CATEGORY_BOX_WIDTH - CATEGORY_BOX_BORDER_WIDTH * 2) * (amount), CATEGORY_BOX_PROGRESS_HEIGHT, new SKPaint { Color = new SKColor(255, 255, 255, 180) });
                xVal += WIDTH + Config.ITEM_PADDING;
            }
        }
    }

    // One per item
    class ItemCache
    {
        public SKBitmap Icon;
        public SKBitmap BlurredIcon;
        public SKBitmap Name;
        public SKBitmap ShortDescription;

        public ItemCache(GlobalCache global, StoreItem item, SectionCache section)
        {
            SKImage icon;
            if (section.Type == DrawType.FEATURED)
            {
                icon = global.Index.GetPackage(item.FeaturedIcon).GetExport<Texture2D>().Image;
            }
            else
            {
                icon = global.Index.GetPackage(item.StandardIcon).GetExport<Texture2D>().Image;
            }
            Icon = icon.SmartResize(section.WIDTH, section.HEIGHT);

            BlurredIcon = ImageExtensions.NewZeroedBitmap(section.WIDTH - Config.BORDER_WIDTH * 2, section.DESCRIPTION_HEIGHT);
            using (var c = new SKCanvas(BlurredIcon))
            {
                c.DrawBitmap(Icon, -Config.BORDER_WIDTH, -(section.HEIGHT - section.PRICE_HEIGHT - section.DESCRIPTION_HEIGHT - 2 * Config.BORDER_WIDTH) - Config.BORDER_WIDTH, new SKPaint { ImageFilter = SKImageFilter.CreateBlur(5, 5) });
            }

            {
                const float letterSpacing = 0;
                var name = item.Name.GetString().ToUpperInvariant();
                float[] widths = new float[name.Length];
                float textWidth = letterSpacing * (name.Length - 1);
                for (int i = 0; i < name.Length; i++)
                {
                    textWidth += widths[i] = section.NamePaint.MeasureText(name[i].ToString());
                }
                Name = ImageExtensions.NewZeroedBitmap((int)(textWidth + section.NameStrokePaint.StrokeWidth * 2), (int)section.NameStrokePaint.FontMetrics.GetHeight());
                using (var c = new SKCanvas(Name))
                {
                    SKPoint[] points = new SKPoint[name.Length];
                    textWidth = 0;
                    for (int i = 0; i < name.Length; i++)
                    {
                        points[i] = new SKPoint(textWidth + section.NameStrokePaint.StrokeWidth, -section.NameStrokePaint.FontMetrics.Ascent);
                        textWidth += widths[i] + letterSpacing;
                    }
                    c.DrawPositionedText(name, points, section.NameStrokePaint);
                    using (var paint = section.NamePaint.Clone())
                    using (var offsetShader = global.GetPalette(item).GetText(section.Type).Offset(textWidth / 2f + section.NameStrokePaint.StrokeWidth, 0))
                    {
                        paint.Shader = offsetShader;
                        c.DrawPositionedText(name, points, paint);
                    }
                }
            }

            {
                var shortDesc = item.ShortDescription.GetString() ?? "Wrap";
                var textWidth = section.ShortDescriptionPaint.MeasureText(shortDesc);
                ShortDescription = ImageExtensions.NewZeroedBitmap((int)(textWidth), (int)section.ShortDescriptionPaint.FontMetrics.GetHeight());
                using (var c = new SKCanvas(ShortDescription))
                {
                    c.DrawText(shortDesc, 0, -section.ShortDescriptionPaint.FontMetrics.Ascent, section.ShortDescriptionPaint);
                }
            }
        }
    }

    // Used for rarities and series
    readonly struct ColorPalette
    {
        // color of series text
        public SKColor SeriesColor { get; }

        // colors for gradients
        public SKColor GradientColor1 { get; }
        public SKColor GradientColor2 { get; }

        // colors for item names
        public SKColor TextColor1 { get; }
        public SKColor TextColor2 { get; }

        readonly SKShader BorderShader;

        readonly SKBitmap[] BackgroundGradients;
        readonly SKShader[] TextGradients;

        public ColorPalette(GlobalCache global, SKColor series, SKColor grad1, SKColor grad2, SKColor text1, SKColor text2)
        {
            SeriesColor = series;
            GradientColor1 = grad1;
            GradientColor2 = grad2;
            TextColor1 = text1;
            TextColor2 = text2;

            BorderShader = SKShader.CreateColorFilter(global.BaseBorderShader, SKColorFilter.CreateBlendMode(ImageExtensions.MultiplyColor(GradientColor1, new SKColor(200, 200, 200)), SKBlendMode.Plus));

            var drawTypes = EnumHelper<DrawType>.Values;
            BackgroundGradients = new SKBitmap[drawTypes.Length];
            TextGradients = new SKShader[drawTypes.Length];

            var gradientColors = new SKColor[] { GradientColor1, GradientColor2 };
            for (int i = 0; i < drawTypes.Length; i++)
            {
                var cache = global.GetSection(drawTypes[i]);
                BackgroundGradients[i] = ImageExtensions.NewZeroedBitmap(cache.WIDTH, cache.HEIGHT);
                using (var c = new SKCanvas(BackgroundGradients[i]))
                {
                    c.DrawRect(new SKRect(0, 0, cache.WIDTH, cache.HEIGHT), new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(cache.BACKGROUND_GRADIENT_CENTER.X * cache.WIDTH, cache.BACKGROUND_GRADIENT_CENTER.Y * cache.HEIGHT), cache.BACKGROUND_GRADIENT_RADIUS, gradientColors, null, SKShaderTileMode.Clamp) });
                }
                TextGradients[i] = GetTextShader(TextColor1, TextColor2, cache.NAME_GRADIENT_POINTS);
            }
        }

        public SKBitmap GetBackground(DrawType type) =>
            BackgroundGradients[(int)type];

        public SKShader GetText(DrawType type) =>
            TextGradients[(int)type];

        public SKShader GetBorder(float x, float y, float offset) =>
            BorderShader.Offset(x + offset * 2, y + offset * -2);

        public SKColor DescriptionBox
        {
            get
            {
                return new SKColor(0, 0, 0, 50);
                GradientColor2.ToHsl(out var h, out var s, out var l);
                l *= .3f;
                return SKColor.FromHsl(h, s, l, 30);
            }
        }

        // unsure if 3000 scales, 
        static SKShader GetTextShader(SKColor a, SKColor b, float[] gradient) =>
            SKShader.CreateRadialGradient(
                new SKPoint(0, 750 + 13),
                // y value originally was config.Dimensions.Y - config.PriceHeight - config.BlurHeight + config.NamePadding - config.NameMetrics.Ascent + 1500f
                1500f,
                new SKColor[]
                {
                a, // bottom
                b,
                SKColors.White,
                SKColors.White,
                a, // top
                },
                gradient,
                SKShaderTileMode.Clamp
            );
    }

    enum DrawType
    {
        FEATURED,
        DAILY,
        COMMUNITY // unused
    }
}

