using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    public interface IProtocol
    {
        Task DeltaAsync(ScreenFrame frame);
        Task StartAsync(ScreenFrame frame);
    }
}