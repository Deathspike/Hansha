using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    // TODO: permessage-deflate. How do I get this? A new WebSocket handler? Protocols should not be aware of compression.
    // TODO: It's currently using a Timeout.Infinite, thus, blocking additional readers for all intents and purposes.
    public class ProtocolHandler : IHandler
    {
        private readonly int _maximumFramesPerSecond;
        private readonly IProtocolProvider _protocolProvider;
        private readonly IScreenProvider _screenProvider;

        #region Constructor

        public ProtocolHandler(int maximumFramesPerSecond, IProtocolProvider protocolProvider, IScreenProvider screenProvider)
        {
            _maximumFramesPerSecond = maximumFramesPerSecond;
            _protocolProvider = protocolProvider;
            _screenProvider = screenProvider;
        }

        #endregion

        #region Implementation of IHandler

        public async Task<bool> HandleAsync(HttpListenerContext context)
        {
            if (context.Request.IsWebSocketRequest)
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var protocolStream = new DebugStream(new WebSocketProtocolStream(webSocketContext));
                var protocol = _protocolProvider.GetProtocol(protocolStream);
                var delayPerFrame = 1000 / _maximumFramesPerSecond;

                using (var screen = _screenProvider.GetScreen())
                {
                    await protocol.StartAsync(screen.GetFrame(Timeout.Infinite));

                    while (true)
                    {
                        var beginTime = DateTime.Now.Ticks;
                        var screenFrame = screen.GetFrame(Timeout.Infinite);

                        if (screenFrame.MovedRegions.Length > 0 || screenFrame.ModifiedRegions.Length > 0)
                        {
                            await protocol.UpdateAsync(screenFrame);
                        }

                        var elapsedTime = (int) ((DateTime.Now.Ticks - beginTime) / TimeSpan.TicksPerSecond);
                        if (elapsedTime < delayPerFrame) await Task.Delay(delayPerFrame - elapsedTime);
                    }
                }
            }

            return false;
        }

        #endregion
    }
}