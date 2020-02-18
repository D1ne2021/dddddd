using System.Threading.Tasks;
using fnbot.shop.Backend.ItemTypes;

namespace fnbot.shop.Backend
{
    public interface IPlatform : IPlugin
    {
        Task<PostResponse> PostItem(IItem item);
    }
}
