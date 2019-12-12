using System;
using System.Threading.Tasks;
using fnbot.shop.Backend.ItemTypes;

namespace fnbot.shop.Backend
{
    public interface IModule : IImport, IDisposable
    {
        int RefreshTime { get; }

        Task<IItem> Post(bool force);

        bool PostsType(ItemType type);
    }
}
