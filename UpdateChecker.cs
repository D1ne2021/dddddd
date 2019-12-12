using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fnbot.shop.Web;

namespace fnbot.shop
{
    public sealed class UpdateChecker : IDisposable
    {
        public const Channel CHANNEL = Channel.PUBLIC;
        public const int BUILD = 0;
        public const string VERSION = "6.9";
        public const string PATCHNOTES = "0666a685-80f2-4b68-80a3-6b7b79e7097c";

        int UpdateInterval = 1000 * 60 * 60 * 6; // 6 hours

        readonly Client Client = new Client();
        readonly Action<VersionData> UpdateEvent;
        readonly CancellationTokenSource CancelToken;
        Task UpdateTask;

        public UpdateChecker(Action<VersionData> updateEvent)
        {
            UpdateEvent = updateEvent;
            CancelToken = new CancellationTokenSource();
            ContinueTask(null);
        }

        public void Dispose()
        {
            CancelToken.Cancel();
            Client.Dispose();
            CancelToken.Dispose();
        }

        void ContinueTask(Task t)
        {
            if (t != null)
            {
                Console.WriteLine(t.Exception);
            }
            UpdateTask = Update(CancelToken.Token);
            _ = UpdateTask.ContinueWith(ContinueTask, TaskContinuationOptions.OnlyOnFaulted);
        }
        async Task Update(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await ForceCheck();
                await Task.Delay(UpdateInterval, token);
            }
        }

        public async Task<bool> ForceCheck()
        {
            var ver = await GetVersion(CHANNEL);
            if (CheckUpdate(ver))
            {
                UpdateEvent(ver);
                return true;
            }
            return false;
        }

        bool CheckUpdate(VersionData ver) => BUILD < ver.build;

        async Task<VersionData> GetVersion(Channel channel)
        {
            return await JsonSerializer.DeserializeAsync<VersionData>((await Client.SendAsync("GET", $"https://fnbot.shop/api/update?c={channel}")).Stream);
        }

        public async Task<string> GetPatchNotesAsync()
        {
            return await (await Client.SendAsync("GET", $"https://fnbot.shop/api/patchnotes/{PATCHNOTES}")).GetStringAsync();
        }

        public async Task<string> GetPatchNotesAsync(VersionData ver)
        {
            return await (await Client.SendAsync("GET", $"https://fnbot.shop/api/patchnotes/{ver.patchnotes}")).GetStringAsync();
        }

        public enum Channel
        {
            PUBLIC
        }
    }

    public struct VersionData
    {
        public string version { get; set; }
        public int build { get; set; }
        public string patchnotes { get; set; }
        public string download { get; set; }
    }
}
