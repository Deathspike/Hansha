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
        private static byte[] _uncompressedBuffer = new byte[1920 * 1080 * 7];

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
                // var count = buffer.DecompressQuick(_uncompressedBuffer);
                // ProcessStart(bitmap, _uncompressedBuffer, 0, count);
                ProcessStart(bitmap, buffer, 0, buffer.Length);

                control.Refresh();
            }));
        }

        private static async Task DispatchDeltaAsync(Control control, Bitmap bitmap, IProtocolStream stream)
        {
            var buffer = await stream.ReceiveAsync();

            control.Invoke(new Action(() =>
            {
                // var count = buffer.DecompressQuick(_uncompressedBuffer);
                // ProcessDelta(bitmap, _uncompressedBuffer, 0, count);

                ProcessDelta(bitmap, buffer, 0, buffer.Length);

                control.Refresh();
            }));
        }

        private static unsafe void ProcessStart(Bitmap bitmap, byte[] buffer, int offset, int count)
        {
            var imageBoundaries = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageData = bitmap.LockBits(imageBoundaries, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            using (var memoryStream = new MemoryStream(buffer))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                var pointer = (byte*) imageData.Scan0;
                var width = binaryReader.ReadInt32();
                var height = binaryReader.ReadInt32();

                while (memoryStream.Position < count)
                {
                    pointer[0] = binaryReader.ReadByte();
                    pointer[1] = binaryReader.ReadByte();
                    pointer[2] = binaryReader.ReadByte();
                    pointer += 4;
                }
            }

            bitmap.UnlockBits(imageData);
        }

        class MovedRegion
        {
            public int FromX { get; set; }
            public int FromY { get; set; }
            public int Height { get; set; }
            public int ToX { get; set; }
            public int ToY { get; set; }
            public int Width { get; set; }
        }

        // TODO: Something in the flow is wrong and causes bleeding pixels when doing a move.
        private static unsafe void CopyRegion(byte *fromBuffer, byte *toBuffer, int totalWidth, MovedRegion region)
        {
            var widthInBytes = totalWidth * 4;

            for (var yOffset = 0; yOffset < region.Height; yOffset++)
            {
                var yOrigin = region.FromY + yOffset;
                var yOriginIndex = yOrigin * widthInBytes;
                var yDestination = region.ToY + yOffset;
                var yDestinationIndex = yDestination * widthInBytes;

                for (var xOffset = 0; xOffset < region.Width; xOffset++)
                {
                    var xOrigin = region.FromX + xOffset;
                    var xOriginIndex = yOriginIndex + xOrigin * 4;
                    var xDestination = region.ToX + xOffset;
                    var xDestinationIndex = yDestinationIndex + xDestination * 4;

                    toBuffer[xDestinationIndex + 0] = fromBuffer[xOriginIndex + 0];
                    toBuffer[xDestinationIndex + 1] = fromBuffer[xOriginIndex + 1];
                    toBuffer[xDestinationIndex + 2] = fromBuffer[xOriginIndex + 2];
                    toBuffer[xDestinationIndex + 3] = fromBuffer[xOriginIndex + 3];
                }
            }
        }

        private static unsafe void ProcessDelta(Bitmap bitmap, byte[] buffer, int offset, int count)
        {
            var imageBoundaries = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageData = bitmap.LockBits(imageBoundaries, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            using (var memoryStream = new MemoryStream(buffer))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                var imagePointer = (byte*)imageData.Scan0;

                // Moved Regions
                var movedRegions = binaryReader.ReadInt32();
                if (movedRegions != 0)
                {
                    var tempBuffer = new byte[bitmap.Width * bitmap.Height * 4];

                    fixed (byte* tempBufferPointer = &tempBuffer[0])
                    {
                        for (var n = 0; n < movedRegions; n++)
                        {
                            var fromX = binaryReader.ReadInt32();
                            var fromY = binaryReader.ReadInt32();
                            var toX = binaryReader.ReadInt32();
                            var toY = binaryReader.ReadInt32();
                            var width = binaryReader.ReadInt32();
                            var height = binaryReader.ReadInt32();
                            var region = new MovedRegion
                            { FromX = fromX, FromY = fromY, Height = height, ToX = toX, ToY = toY, Width = width };

                            CopyRegion(imagePointer, tempBufferPointer, bitmap.Width, region);
                            CopyRegion(tempBufferPointer, imagePointer, bitmap.Width, region);
                        }
                    }
                }

                // Modified Regions
                while (memoryStream.Position < count)
                {
                    var pointer = imagePointer + binaryReader.ReadInt32();
                    pointer[0] = binaryReader.ReadByte();
                    pointer[1] = binaryReader.ReadByte();
                    pointer[2] = binaryReader.ReadByte();
                }
            }

            bitmap.UnlockBits(imageData);
        }
    }
}