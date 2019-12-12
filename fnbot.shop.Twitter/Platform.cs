using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using fnbot.shop.Backend;
using fnbot.shop.Backend.Configuration;
using fnbot.shop.Backend.ItemTypes;
using fnbot.shop.Web;

namespace fnbot.shop.Twitter
{
    public sealed class Platform : IPlatform
    {
        readonly PlatformConfig config = new PlatformConfig();
        public IConfig Config => config;

        const string STATUS_UPDATE = "https://api.twitter.com/1.1/statuses/update.json";
        const string MEDIA_UPLOAD = "https://upload.twitter.com/1.1/media/upload.json";

        internal Client Client = new Client();

        public void Dispose()
        {
            Client.Dispose();
        }

        public async Task<PostResponse> PostItem(IItem item)
        {
            if (config.OAuthToken == null)
            {
                throw new UnauthorizedAccessException("Twitter is not logged in!");
            }
            return await (item switch
            {
                ItemText text => PostText(text),
                ItemImage img => PostImage(img),
                ItemAlbum album => PostAlbum(album),
                ItemGif gif => PostGif(gif),
                ItemVideo video => PostVideo(video),
                _ => throw new ArgumentException($"Unknown item type {item.GetType()}", nameof(item))
            });
        }

        async Task<PostResponse> PostText(ItemText text)
        {
            var dict = new Dictionary<string, string> { { "status", text.Text } };
            Client.SetHeader("Authorization", await GetOAuthHeader("POST", STATUS_UPDATE, dict));
            await Client.SendFormAsync("POST", STATUS_UPDATE, dict, false);
            return PostResponse.SUCCESS;
        }
        Task<PostResponse> PostImage(ItemImage image) =>
            PostMedia(image.Stream, image.Caption);
        async Task<PostResponse> PostAlbum(ItemAlbum text)
        {
            return PostResponse.UNSUPPORTED_TYPE;
        }
        Task<PostResponse> PostGif(ItemGif gif) =>
            PostMedia(gif.Stream, gif.Caption);
        async Task<PostResponse> PostVideo(ItemVideo video)
        {
            var mediaId = await UploadChunkedMedia(video.Stream);
            if (mediaId == null)
                return PostResponse.UNAUTHORIZED;
            return await PostMedia(mediaId, video.Caption);
        }

        async Task<PostResponse> PostMedia(Stream stream, string caption)
        {
            var mediaId = await UploadMedia(stream);
            if (mediaId == null)
                return PostResponse.UNAUTHORIZED;
            return await PostMedia(mediaId, caption);
        }

        async Task<PostResponse> PostMedia(string mediaId, string caption)
        {
            var dict = new Dictionary<string, string> { { "status", caption }, { "media_ids", mediaId } };
            Client.SetHeader("Authorization", await GetOAuthHeader("POST", STATUS_UPDATE, dict));
            await Client.SendFormAsync("POST", STATUS_UPDATE, dict, false);
            return PostResponse.SUCCESS;
        }

        async Task<string> UploadMedia(Stream stream)
        {
            stream.Position = 0;
            Client.SetHeader("Authorization", await GetOAuthHeader("POST", MEDIA_UPLOAD, null));
            var msg = await Client.SendMultipartAsync("POST", MEDIA_UPLOAD, new Dictionary<string, object> { { "media", stream } });
            var mediaId = (await JsonSerializer.DeserializeAsync<MediaObj>(msg.Stream)).media_id_string;
            if (string.IsNullOrWhiteSpace(mediaId))
            {
                Console.WriteLine($"Invalid token!");
                return null;
            }
            return mediaId;
        }

        async Task<string> UploadChunkedMedia(Stream stream)
        {
            stream.Position = 0;
            var dict = new Dictionary<string, string> { { "command", "INIT" }, { "media_type", "video/mp4" }, { "total_bytes", stream.Length.ToString() } };
            Client.SetHeader("Authorization", await GetOAuthHeader("POST", MEDIA_UPLOAD, dict));
            var msg = await Client.SendFormAsync("POST", MEDIA_UPLOAD, dict);
            var mediaId = (await JsonSerializer.DeserializeAsync<MediaObj>(msg.Stream)).media_id_string;
            if (string.IsNullOrWhiteSpace(mediaId))
            {
                Console.WriteLine($"Invalid token!");
                return null;
            }

            Client.SetHeader("Authorization", await GetOAuthHeader("POST", MEDIA_UPLOAD, null));
            await Client.SendMultipartAsync("POST", MEDIA_UPLOAD, new Dictionary<string, object> { { "command", "APPEND" }, { "media_id", mediaId }, { "segment_index", "0" }, { "media", new MultipartFile(stream, "media") } });

            dict = new Dictionary<string, string> { { "command", "FINALIZE" }, { "media_id", mediaId } };
            Client.SetHeader("Authorization", await GetOAuthHeader("POST", MEDIA_UPLOAD, dict));
            await Client.SendFormAsync("POST", MEDIA_UPLOAD, dict);
            return mediaId;
        }

        static JsonSerializerOptions HeaderSerializer;
        async Task<string> GetOAuthHeader(string method, string url, Dictionary<string, string> data, bool authorized = true)
        {
            if (HeaderSerializer == null)
            {
                HeaderSerializer = new JsonSerializerOptions { IgnoreNullValues = true };
            }
            var resp = await Client.SendJsonAsync("POST", "https://fnbot.shop/api/twitreq", JsonSerializer.Serialize(new HeaderReq {
                method = method,
                url = url,
                @params = data,
                token = authorized ? config.OAuthToken : null,
                secret = authorized ? config.OAuthSecret : null,
            }));
            return await resp.GetStringAsync();
        }

        public IItemInfo SupportsType(ItemType type) =>
            type switch
            {
                ItemType.TEXT => new ItemTextInfo(280, null),
                ItemType.IMAGE => new ItemImageInfo((ItemTextInfo)SupportsType(ItemType.TEXT), -1, -1, -1, -1, 1024 * 1024 * 8, new ItemImageInfo.MediaType[] { ItemImageInfo.MediaType.GIF, ItemImageInfo.MediaType.JPG, ItemImageInfo.MediaType.PNG, ItemImageInfo.MediaType.WEBP }),
                ItemType.ALBUM => new ItemAlbumInfo((ItemTextInfo)SupportsType(ItemType.TEXT), -1, -1, -1, -1, 1024 * 1024 * 8, new ItemImageInfo.MediaType[] { ItemImageInfo.MediaType.GIF, ItemImageInfo.MediaType.JPG, ItemImageInfo.MediaType.PNG, ItemImageInfo.MediaType.WEBP }, 1),
                ItemType.GIF => new ItemGifInfo((ItemTextInfo)SupportsType(ItemType.TEXT), -1, -1, -1, -1, 1024 * 1024 * 8, new ItemGifInfo.MediaType[] { ItemGifInfo.MediaType.GIF }, -1),
                ItemType.VIDEO => new ItemVideoInfo((ItemTextInfo)SupportsType(ItemType.TEXT), -1, -1, -1, -1, 1024 * 1024 * 8, -1, -1, new ItemVideoInfo.MediaType[] { ItemVideoInfo.MediaType.MOV, ItemVideoInfo.MediaType.MP4, ItemVideoInfo.MediaType.WEBM }),
                _ => null,
            };

        struct MediaObj
        {
            public string media_id_string { get; set; }
        }

        struct HeaderReq
        {
            public string method { get; set; }
            public string url { get; set; }
            public string token { get; set; }
            public string secret { get; set; }
            public Dictionary<string, string> @params {get; set;}
        }
    }
}
