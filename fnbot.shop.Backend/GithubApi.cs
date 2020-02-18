using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using fnbot.shop.Web;

namespace fnbot.shop.Backend
{
    class GithubApi
    {
        Client client;

        static GithubApi Singleton;

        public async Task<GithubApi> GetApiAsync()
        {
            if (Singleton != null)
            {
                Singleton = new GithubApi();
                await Singleton.InitializeAsync();
            }
            return Singleton;
        }

        private async Task InitializeAsync()
        {
            client = new Client();
            client.SetHeader("User-Agent", "fnbot.shop (WorkingRobot)");
            JsonSerializer.Deserialize<RatelimitInfo>(await client.GetAsync("https://api.github.com/rate_limit"));
        }

        public async Task<Release?> GetLatestReleaseAsync(string repo)
        {
            var info = JsonSerializer.Deserialize<ReleaseInfo>(await client.GetAsync($"https://api.github.com/repos/{repo}/releases/latest"));
            if (info.Assets.Length == 0)
            {
                return null;
            }
            ReleaseInfo.AssetInfo asset = null;
            {
                for (int i = 0; i < info.Assets.Length; ++i)
                {
                    if (info.Assets[i].Name.EndsWith(".fnp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        asset = info.Assets[i];
                    }
                }
                if (asset == null)
                {
                    return null;
                }
            }
            return new Release(repo, info.TagName, info.Body, asset.BrowserDownloadUrl, asset.DownloadCount, info.PublishedAt);
        }

        sealed class RatelimitInfo
        {
            [JsonPropertyName("resources")]
            public ResourceInfo Resources { get; set; }

            [JsonPropertyName("rate")]
            public RateInfo Rate { get; set; }

            public sealed class ResourceInfo
            {
                [JsonPropertyName("core")]
                public RateInfo Core { get; set; }
                [JsonPropertyName("share")]
                public RateInfo Share { get; set; }
                [JsonPropertyName("graphql")]
                public RateInfo GraphQL { get; set; }
                [JsonPropertyName("integration_manifest")]
                public RateInfo IntegrationManifest { get; set; }
                [JsonPropertyName("source_import")]
                public RateInfo SourceImport { get; set; }
            }

            public sealed class RateInfo
            {
                [JsonPropertyName("limit")]
                public string Limit { get; set; }
                [JsonPropertyName("remaining")]
                public string Remaining { get; set; }
                [JsonPropertyName("reset")]
                public DateTimeOffset Reset { get; set; }
            }
        }

        sealed class ReleaseInfo
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("assets_url")]
            public string AssetsUrl { get; set; }
            [JsonPropertyName("upload_url")]
            public string UploadUrl { get; set; }
            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; }
            [JsonPropertyName("id")]
            public int Id { get; set; }
            [JsonPropertyName("node_id")]
            public string NodeId { get; set; }
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }
            [JsonPropertyName("target_commitish")]
            public string TargetCommitish { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("draft")]
            public bool Draft { get; set; }
            [JsonPropertyName("author")]
            public AuthorInfo Author { get; set; }
            [JsonPropertyName("prerelease")]
            public bool Prerelease { get; set; }
            [JsonPropertyName("created_at")]
            public DateTimeOffset CreatedAt { get; set; }
            [JsonPropertyName("published_at")]
            public DateTimeOffset PublishedAt { get; set; }
            [JsonPropertyName("assets")]
            public AssetInfo[] Assets { get; set; }
            [JsonPropertyName("tarball_url")]
            public string TarUrl { get; set; }
            [JsonPropertyName("zipball_url")]
            public string ZipUrl { get; set; }
            [JsonPropertyName("body")]
            public string Body { get; set; }

            public sealed class AuthorInfo
            {
                [JsonPropertyName("login")]
                public string Login { get; set; }
                [JsonPropertyName("id")]
                public int Id { get; set; }
                [JsonPropertyName("node_id")]
                public string NodeId { get; set; }
                [JsonPropertyName("avatar_url")]
                public string AvatarUrl { get; set; }
                [JsonPropertyName("gravatar_id")]
                public string GravatarId { get; set; }
                [JsonPropertyName("url")]
                public string Url { get; set; }
                [JsonPropertyName("html_url")]
                public string HtmlUrl { get; set; }
                [JsonPropertyName("followers_url")]
                public string FollowersUrl { get; set; }
                [JsonPropertyName("following_url")]
                public string FollowingUrl { get; set; }
                [JsonPropertyName("gists_url")]
                public string GistsUrl { get; set; }
                [JsonPropertyName("starred_url")]
                public string StarredUrl { get; set; }
                [JsonPropertyName("subscriptions_url")]
                public string SubscriptionsUrl { get; set; }
                [JsonPropertyName("organizations_url")]
                public string OrganizationsUrl { get; set; }
                [JsonPropertyName("repos_url")]
                public string ReposUrl { get; set; }
                [JsonPropertyName("events_url")]
                public string EventsUrl { get; set; }
                [JsonPropertyName("received_events_url")]
                public string RecievedEventsUrl { get; set; }
                [JsonPropertyName("type")]
                public string Type { get; set; }
                [JsonPropertyName("site_admin")]
                public bool SiteAdmin { get; set; }
            }

            public sealed class AssetInfo
            {
                [JsonPropertyName("url")]
                public string Url { get; set; }
                [JsonPropertyName("id")]
                public int Id { get; set; }
                [JsonPropertyName("node_id")]
                public string NodeId { get; set; }
                [JsonPropertyName("name")]
                public string Name { get; set; }
                [JsonPropertyName("label")]
                public string Label { get; set; }
                [JsonPropertyName("uploader")]
                public AuthorInfo Uploader { get; set; }
                [JsonPropertyName("content_type")]
                public string ContentType { get; set; }
                [JsonPropertyName("state")]
                public string State { get; set; }
                [JsonPropertyName("size")]
                public long Size { get; set; }
                [JsonPropertyName("download_count")]
                public int DownloadCount { get; set; }
                [JsonPropertyName("created_at")]
                public DateTimeOffset CreatedAt { get; set; }
                [JsonPropertyName("updated_at")]
                public DateTimeOffset UpdatedAt { get; set; }
                [JsonPropertyName("browser_download_url")]
                public string BrowserDownloadUrl { get; set; }
            }
        }

        public readonly struct Release
        {
            public string Repo { get; }
            public string ReleaseVersion { get; }
            public string PatchNotes { get; }
            public string DownloadUrl { get; }
            public int DownloadCount { get; }
            public DateTimeOffset UpdatedAt { get; }

            public Release(string repo, string version, string patchnotes, string url, int count, DateTimeOffset updated)
            {
                Repo = repo;
                ReleaseVersion = version;
                PatchNotes = patchnotes;
                DownloadUrl = url;
                DownloadCount = count;
                UpdatedAt = updated;
            }
        }
    }
}
