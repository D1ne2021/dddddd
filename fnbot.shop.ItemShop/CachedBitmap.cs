using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace fnbot.shop.ItemShop
{
    readonly unsafe struct CachedBitmap
    {
        public const int BPP = 4; // rgba or argb or abgra or fpakentry
        public readonly int Width;
        public readonly int Height;
        public readonly byte* Pointer;

        public CachedBitmap(int w, int h, IntPtr? buffer = null)
        {
            Width = w;
            Height = h;
            Pointer = (byte*)(buffer ?? Marshal.AllocHGlobal(w * h * BPP)).ToPointer();
        }

        public CachedBitmap(SKBitmap bmp)
        {
            if (bmp.BytesPerPixel != BPP)
                throw new ArgumentException($"Bitmap has {bmp.BytesPerPixel} BPP, only supports {BPP}");
            Width = bmp.Width;
            Height = bmp.Height;
            Pointer = (byte*)bmp.GetPixels().ToPointer();
        }

        public void Copy(in CachedBitmap dstbmp, in Rect src, in Point dst)
        {
            for (int h = 0; h < src.H; h++)
            {
                MemCopy(Pointer + GetOffset(src.X, src.Y + h, Width), dstbmp.Pointer + GetOffset(dst.X, dst.Y + h, dstbmp.Width), src.W * BPP);
            }
        }

        public void FullCopy(in CachedBitmap dstbmp)
        {
            if (dstbmp.Width != Width || dstbmp.Height != Height)
                throw new ArgumentException($"Widths or heights aren't equal");
            MemCopy(Pointer, dstbmp.Pointer, Width * Height * BPP);
        }

        static int GetOffset(int x, int y, int w) =>
            (y * w + x) * BPP;

        // [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        // static unsafe extern IntPtr MemCpy(void* dest, void* src, int count);

        static void MemCopy(void* src, void* dst, int len) =>
            Unsafe.CopyBlock(dst, src, (uint)len); // ~1900ms
                                                   // MemCpy(dst, src, len); // ~2150ms
                                                   // Buffer.MemoryCopy(src, dst, len, len); // ~2100ms
                                                   // timings are for copying a 720p bitmap 500 times
    }
}
