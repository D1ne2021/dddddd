using System;
using System.Threading.Tasks;
using fnbot.shop.Backend.ItemTypes;

namespace fnbot.shop.Backend
{
    public interface IPlatform : IImport, IDisposable
    {
        Task<PostResponse> PostItem(IItem item);

        IItemInfo SupportsType(ItemType type);
    }
}
