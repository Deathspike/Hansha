using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hansha.TestProtocol
{
    // Hard-coded to 1920x1080. Not a very nice implementation at all.
    public class TestProtocolCore
    {
        public static async Task Run(Control control, Bitmap bitmap)
        {
            using (var webSocket = new ClientWebSocket())
            {
                var stream = new WebSocketStream(webSocket);

                await webSocket.ConnectAsync(new Uri("ws://localhost:5050/ws"), CancellationToken.None);

                await DispatchStartAsync(control, bitmap, stream);

                while (true)
                {
                    await DispatchDeltaAsync(control, bitmap, stream);
                }
            }
        }

        private static async Task DispatchStartAsync(Control control, Bitmap bitmap, IProtocolStream stream)
        {
            var buffer = await stream.ReceiveAsync();

            control.Invoke(new Action(() =>
            {
                ProcessStart(bitmap, buffer);
                control.Refresh();
            }));
        }

        private static async Task DispatchDeltaAsync(Control control, Bitmap bitmap, IProtocolStream stream)
        {
            var buffer = await stream.ReceiveAsync();

            control.Invoke(new Action(() =>
            {
                ProcessDelta(bitmap, buffer);
                control.Refresh();
            }));
        }

        private static unsafe void ProcessStart(Bitmap bitmap, byte[] buffer)
        {
            var imageBoundaries = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageData = bitmap.LockBits(imageBoundaries, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            using (var memoryStream = new MemoryStream(buffer))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                var pointer = (byte*) imageData.Scan0;
                var width = binaryReader.ReadUInt16();
                var height = binaryReader.ReadUInt16();

                while (memoryStream.Position < buffer.Length)
                {
                    pointer[0] = binaryReader.ReadByte();
                    pointer[1] = binaryReader.ReadByte();
                    pointer[2] = binaryReader.ReadByte();
                    pointer += 4;
                }
            }

            bitmap.UnlockBits(imageData);
        }

        private static unsafe void ProcessDelta(Bitmap bitmap, byte[] buffer)
        {
            var imageBoundaries = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageData = bitmap.LockBits(imageBoundaries, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            using (var memoryStream = new MemoryStream(buffer))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                var basePointer = (byte*) imageData.Scan0;

                while (memoryStream.Position < buffer.Length)
                {
                    var pointer = basePointer + binaryReader.ReadUInt32();
                    pointer[0] = binaryReader.ReadByte();
                    pointer[1] = binaryReader.ReadByte();
                    pointer[2] = binaryReader.ReadByte();
                }
            }

            bitmap.UnlockBits(imageData);
        }
    }
}