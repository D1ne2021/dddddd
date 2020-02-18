using System.Threading.Tasks;
using fnbot.shop.Backend.ItemTypes;

namespace fnbot.shop.Backend
{
    public interface IModule : IPlugin
    {
        int RefreshTime { get; }

        Task<IItem> Post(bool force);
    }
}
