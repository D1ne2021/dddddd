using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using fnbot.shop.Fortnite;
using fnbot.shop.Video;
using SkiaSharp;

namespace fnbot.shop.ItemShop
{
    class Generator
    {
        Storefront Store;
        string Background;
        public Generator(Storefront store, string background)
        {
            Store = store;
            Background = background;
        }

        public void WriteToStream(MemoryStream output)
        {
            var converter = new Converter(PakSupplier.Index);
            StoreItem[] weekly = new StoreItem[Store.Weekly.Length];
            for (int i = 0; i < weekly.Length; i++)
            {
                try
                {
                    weekly[i] = converter.ConvertItem(Store.Weekly[i]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            StoreItem[] daily = new StoreItem[Store.Daily.Length];
            for (int i = 0; i < daily.Length; i++)
            {
                try
                {
                    daily[i] = converter.ConvertItem(Store.Daily[i]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw e;
                }
            }
            // number of featured categories
            var weeklies = Converter.GetCategorySlots(weekly, out var count, out var sizes, out var size);
            Config.SelectedCategory = count;
            var gc = new GlobalCache(converter.Index);

            // TODO: making it any larger doesn't affect the text or icon resolution :(
            // might have to do with the canvas only writing 1080p pixels on a 4k (e.g) image
            const int w = 1280;
            const int h = 720;
            using var ret = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var stream = new VideoOutputStream(ret, 30, 1024 * 1024 * 5, output, true);
            using (var c = new SKCanvas(ret))
            {
                if (!string.IsNullOrWhiteSpace(Background) && File.Exists(Background))
                {
                    try
                    {
                        // TODO: make this permanent or part of drawing the cache (maybe?) just do something
                        using var bmp = SKBitmap.Decode(Background);
                        c.DrawBitmap(bmp, new SKRect(0, 0, w, h), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                c.SetMatrix(SKMatrix.MakeScale(w / 1920f, h / 1080f));
                for (int i = 0; i < size; i++)
                {
                    gc.GetSection(DrawType.DAILY).Draw(c, daily);
                    gc.GetSection(DrawType.FEATURED).Draw(c, weeklies[i]);
                    gc.GetSection(DrawType.FEATURED).InitializeCategory(c, weeklies[i], sizes, i + 1, 0);
                    float tickAmt = 1 / 150f;
                    for (int j = 0; j < 150; j++)
                    {
                        gc.GetSection(DrawType.DAILY).DrawBorder(c, daily, j);
                        gc.GetSection(DrawType.FEATURED).DrawBorder(c, weeklies[i], j + i * 150);
                        stream.EncodeFrame();
                        gc.GetSection(DrawType.FEATURED).TickCategory(c, weeklies[i], sizes, j * tickAmt, tickAmt);
                    }
                }
            }
            /*
            using var bmp = new SKBitmap(1280, 720, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var stream = new VideoOutputStream(bmp, 30, 1024 * 1024 * 4, output, true);
            using var c = new SKCanvas(bmp);
            */
        }
    }
}
