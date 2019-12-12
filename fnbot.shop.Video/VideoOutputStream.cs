using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace fnbot.shop.Video
{
    public sealed class VideoOutputStream : IDisposable
    {
        readonly IntPtr Encoder;
        readonly IntPtr Muxer;
        readonly IntPtr Mp4wr;

        readonly IntPtr Image;
        readonly ulong ImagePitch;
        readonly uint Width;
        readonly uint Height;
        readonly Stream Output;
        readonly bool LeaveOpen;
        readonly Natives.MuxerCallback callback;

        public VideoOutputStream(SKBitmap bitmap, uint fps, ulong bitrate, Stream output, bool leaveOpen = false)
        {
            if (bitmap.ColorType != SKColorType.Rgba8888)
                throw new ArgumentException("Must be SKColorType.Rgba8888", nameof(bitmap));

            Encoder = Natives.CreateEncoder(Natives.Format.NVPIPE_RGBA32, Natives.Codec.NVPIPE_H264, Natives.Compression.NVPIPE_LOSSY, bitrate, fps, (uint)bitmap.Width, (uint)bitmap.Height);
            var error = Marshal.PtrToStringAnsi(Natives.GetEncoderError(Encoder));
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new NotSupportedException(error);
            }
            Image = bitmap.GetPixels();
            ImagePitch = (ulong)bitmap.RowBytes;
            Width = (uint)bitmap.Width;
            Height = (uint)bitmap.Height;
            LeaveOpen = leaveOpen;
            Output = output;
            callback = WriteCallback;
            Muxer = Natives.OpenMuxer(0, callback);
            Mp4wr = Natives.InitH26XMuxer(Muxer, bitmap.Width, bitmap.Height, 0);
        }

        void WriteCallback(long offset, IntPtr buffer, uint size)
        {
            Output.Position = offset;
            byte[] buf = new byte[size];
            Marshal.Copy(buffer, buf, 0, (int)size);
            Output.Write(buf, 0, (int)size);
        }

        public void EncodeFrame(bool forceIFrame = false) =>
            Natives.EncodeFrame(Encoder, Image, ImagePitch, Mp4wr, Width, Height, forceIFrame);

        public void Dispose()
        {
            Natives.CloseEncoder(Encoder);
            Natives.CloseMuxer(Muxer);
            Marshal.FreeHGlobal(Mp4wr);
            if (!LeaveOpen)
            {
                Output.Close();
                Output.Dispose();
            }
        }
    }
}
