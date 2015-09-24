﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hansha
{
    public class WebSocketProtocolStream : IProtocolStream
    {
        private readonly byte[] _receiveBuffer;
        private readonly WebSocket _webSocket;

        #region Constructor

        public WebSocketProtocolStream(WebSocket webSocket)
        {
            _receiveBuffer = new byte[1024];
            _webSocket = webSocket;
        }

        #endregion

        #region Implementation of IProtocolStream

        public Task CloseAsync()
        {
            return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        public async Task<byte[]> ReceiveAsync()
        {
            using (var memoryStream = new MemoryStream())
            {
                var buffer = new ArraySegment<byte>(_receiveBuffer);

                while (!(await _webSocket.ReceiveAsync(buffer, CancellationToken.None)).EndOfMessage)
                {
                    memoryStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                }

                return memoryStream.ToArray();
            }
        }

        public Task SendAsync(byte[] buffer, int offset, int count)
        {
            var arraySegment = new ArraySegment<byte>(buffer, offset, count);

            return _webSocket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        #endregion
    }
}