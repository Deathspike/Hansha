using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hansha
{
    public class WebSocketProtocolStream : IProtocolStream
    {
        private readonly WebSocketContext _context;

        #region Constructor

        public WebSocketProtocolStream(WebSocketContext context)
        {
            _context = context;
        }

        #endregion

        #region Implementation of IProtocolStream

        public Task CloseAsync()
        {
            return _context.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        public async Task<byte[]> ReceiveAsync()
        {
            using (var memoryStream = new MemoryStream())
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);

                while (!(await _context.WebSocket.ReceiveAsync(buffer, CancellationToken.None)).EndOfMessage)
                {
                    memoryStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                }

                return memoryStream.ToArray();
            }
        }

        public Task SendAsync(byte[] buffer, int offset, int count)
        {
            var arraySegment = new ArraySegment<byte>(buffer, offset, count);

            return _context.WebSocket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        #endregion
    }
}