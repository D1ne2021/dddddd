using System.IO;

namespace fnbot.shop.Backend.Configuration
{
    public interface IConfig
    {
        void SaveConfig(Stream stream);
        void LoadConfig(Stream stream);
    }
}
