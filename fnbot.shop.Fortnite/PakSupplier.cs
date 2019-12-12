using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PakReader.Pak;
using PakReader.Parsers.Objects;

namespace fnbot.shop.Fortnite
{
    public static class PakSupplier
    {
        public static PakIndex Index { get; private set; }

        public static async Task InitializeOffline(string directory)
        {
            Index = new PakIndex(directory, true, false, null);
            Index.UseKey(FGuid.Zero, await AuthSupplier.GetKeyAsync());
            foreach(var (guid, key) in await AuthSupplier.GetKeychainAsync())
            {
                Index.UseKey(guid, key);
            }
        }
    }
}
