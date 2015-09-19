using System;
using System.Net;
using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
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
                var protocolStream = new WebSocketProtocolStream(webSocketContext);
                var protocol = _protocolProvider.GetProtocol(protocolStream);
                var delayPerFrame = 1000 / _maximumFramesPerSecond;

                using (var screen = _screenProvider.GetScreen())
                {
                    await protocol.StartAsync(screen.GetFrame(0));

                    while (true)
                    {
                        var beginTime = DateTime.Now.Ticks;
                        var screenFrame = screen.GetFrame(0);

                        await protocol.UpdateAsync(screenFrame);

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