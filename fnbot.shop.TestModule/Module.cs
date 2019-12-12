using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using fnbot.shop.Backend;
using fnbot.shop.Backend.Configuration;
using fnbot.shop.Backend.ItemTypes;
using fnbot.shop.Web;

namespace fnbot.shop.TestPlatform
{
    class Module : IModule
    {
        public int RefreshTime => 5000;

        public IConfig Config => null;

        Client Client = new Client();

        public void Dispose()
        {
            Client.Dispose();
        }

        public async Task<IItem> Post()
        {
            if (new Random().NextDouble() > .25)
            {
                var data = await (await Client.SendJsonAsync("POST", "https://api.mojang.com/orders/statistics", "{\"metricKeys\":[\"item_sold_minecraft\"]}")).GetStringAsync();
                return new ItemText(data);
            }
            return null;
        }

        public bool PostsType(ItemType type) =>
            type switch
            {
                ItemType.TEXT => true,
                ItemType.IMAGE => false,
                ItemType.ALBUM => false,
                ItemType.GIF => false,
                ItemType.VIDEO => false,
                _ => false,
            };
    }
}
