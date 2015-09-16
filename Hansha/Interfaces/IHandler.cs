using System.Net;
using System.Threading.Tasks;

namespace Hansha
{
    public interface IHandler
    {
        Task<bool> HandleAsync(HttpListenerContext context);
    }
}