using System;
using Hansha.Core.DesktopDuplication;

namespace Hansha
{
    public class Program
    {
        public static void Main()
        {
            // The protocol on the client-side must match.
            var protocolProvider = new SimpleProtocolProvider();
            var screenProvider = new DesktopDuplicationScreenProvider();

            using (var server = new Server("http://+:5050/"))
            {
                EventLoop.Pump(() =>
                {
                    server.Add(new ProtocolHandler(30, protocolProvider, screenProvider));
                    server.Add(new StaticHandler("Content"));
                    EventLoop.Run(server.RunAsync);
                });
            }
        }
    }
}