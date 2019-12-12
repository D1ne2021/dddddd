using System;
using System.Runtime.InteropServices;

namespace fnbot.shop.Video
{
    internal static class Natives
    {
        [DllImport("NvPipe.dll", EntryPoint = "NvPipe_CreateEncoder")]
        public static extern IntPtr CreateEncoder(Format format, Codec codec, Compression compression, ulong bitrate, uint targetFrameRate, uint width, uint height);

        [DllImport("NvPipe.dll", EntryPoint = "NvPipe_SetBitrate")]
        public static extern void SetBitrate(IntPtr nvp, ulong bitrate, uint targetFrameRate);

        [DllImport("NvPipe.dll", EntryPoint = "NvPipe_Encode")]
        public static extern void EncodeFrame(IntPtr nvp, IntPtr src, ulong srcPitch, IntPtr h, uint width, uint height, bool forceIFrame);

        [DllImport("NvPipe.dll", EntryPoint = "NvPipe_Destroy")]
        public static extern void CloseEncoder(IntPtr nvp);

        [DllImport("NvPipe.dll", EntryPoint = "NvPipe_GetError")]
        public static extern IntPtr GetEncoderError(IntPtr nvp);

        [DllImport("NvPipe.dll", EntryPoint = "MP4E__open")]
        public static extern IntPtr OpenMuxer(int sequential_mode_flag, MuxerCallback write_callback);

        [DllImport("NvPipe.dll", EntryPoint = "CUSTOM_mp4_h26x_write_init")]
        public static extern IntPtr InitH26XMuxer(IntPtr mux, int width, int height, int is_hevc);

        [DllImport("NvPipe.dll", EntryPoint = "MP4E__close")]
        public static extern ulong CloseMuxer(IntPtr mux);

        public delegate void MuxerCallback(long offset, IntPtr buffer, uint size);

        public enum Codec
        {
            NVPIPE_H264,
            NVPIPE_HEVC
        }

        public enum Compression
        {
            NVPIPE_LOSSY,
            NVPIPE_LOSSLESS
        }

        public enum Format
        {
            NVPIPE_RGBA32,
            NVPIPE_UINT4,
            NVPIPE_UINT8,
            NVPIPE_UINT16,
            NVPIPE_UINT32
        }
    }
}
