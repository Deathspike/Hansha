using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    public interface IProtocol
    {
        Task StartAsync(ScreenFrame frame);
        Task UpdateAsync(ScreenFrame frame);
    }
}