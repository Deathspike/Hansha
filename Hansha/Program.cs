using System;
using Hansha.Core;
using Hansha.Core.BitBlt;
using Hansha.Core.DesktopDuplication;

namespace Hansha
{
    public class Program
    {
        private static IScreenProvider _CreateScreenProvider()
        {
            if (_IsWindows8OrAbove())
            {
                return new DesktopDuplicationScreenProvider();
            }

            return new BitBltScreenProvider();
        }

        private static bool _IsWindows8OrAbove()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT && ((os.Version.Major == 6 && os.Version.Minor >= 2) || (os.Version.Major > 6));
        }

        public static void Main()
        {
            // The protocol on the client-side must match.
            var protocolProvider = new SimpleProtocolProvider();

            using (var server = new Server("http://+:5050/"))
            {
                EventLoop.Pump(() =>
                {
                    server.Add(new ProtocolHandler(30, protocolProvider, _CreateScreenProvider()));
                    server.Add(new StaticHandler("Content"));
                    EventLoop.Run(server.RunAsync);
                });
            }
        }
    }
}