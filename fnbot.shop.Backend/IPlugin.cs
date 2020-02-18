using System;
using fnbot.shop.Backend.Configuration;

namespace fnbot.shop.Backend
{
    public interface IPlugin : IDisposable
    {
        IConfig Config { get; }
    }
}
