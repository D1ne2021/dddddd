using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using fnbot.shop.Backend;

namespace fnbot.shop
{
    sealed class BitmapImageCache
    {
        readonly Dictionary<Guid, BitmapImage> ImageCache = new Dictionary<Guid, BitmapImage>();

        public BitmapImage GetImage<T>(PluginData<T> plugin) where T : IPlugin
        {
            if (!ImageCache.TryGetValue(plugin.Plugin.GUID, out var image))
            {
                image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = plugin.Plugin.Icon;
                plugin.Plugin.Icon.Position = 0;
                image.EndInit();
                ImageCache[plugin.Plugin.GUID] = image;
            }
            return image;
        }

        public bool RemoveImage<T>(PluginData<T> plugin) where T : IPlugin =>
            ImageCache.Remove(plugin.Plugin.GUID);
    }
}
