using System.Threading.Tasks;

namespace Hansha
{
    public interface IProtocolStream
    {
        Task CloseAsync();
        Task<byte[]> ReceiveAsync();
        Task SendAsync(byte[] bytes);
    }
}