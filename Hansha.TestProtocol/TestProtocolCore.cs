using System;
using System.CodeDom;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hansha.TestProtocol
{
    // Hard-coded to 1920x1080. Not a very nice implementation at all.
    // TODO: Optimize buffer usage. Why do we allocate a new buffer each time?
    public class TestProtocolCore
    {
        private static byte[] _bufferUncompressed = new byte[1920 * 1080 * 7];

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
                int len = buffer.DecompressQuick(_bufferUncompressed);

                ProcessStart(bitmap, _bufferUncompressed, 0, len);

                control.Refresh();
            }));
        }

        private static async Task DispatchDeltaAsync(Control control, Bitmap bitmap, IProtocolStream stream)
        {
            var buffer = await stream.ReceiveAsync();

            control.Invoke(new Action(() =>
            {
                int len = buffer.DecompressQuick(_bufferUncompressed);

                ProcessDelta(bitmap, _bufferUncompressed, 0, len);

                control.Refresh();
            }));
        }

        private static unsafe void ProcessStart(Bitmap bitmap, byte[] buffer, int offset, int count)
        {
            var imageBoundaries = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageData = bitmap.LockBits(imageBoundaries, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            var reader = new QuickBinaryReader(buffer);
            var pointer = (byte*) imageData.Scan0;
            var width = reader.ReadUnsignedVariableLength();
            var height = reader.ReadUnsignedVariableLength();

            while (reader.Position < count)
            {
                var bg = reader.ReadByte();
                var gr = reader.ReadByte();
                pointer[0] = (byte)(bg & 248);
                pointer[1] = (byte)((bg & 7) << 5 | (gr & 7) << 2);
                pointer[2] = (byte)(gr & 248);
                pointer += 4;
            }

            bitmap.UnlockBits(imageData);
        }

        class MovedRegion
        {
            public uint FromX { get; set; }
            public uint FromY { get; set; }
            public uint Height { get; set; }
            public uint ToX { get; set; }
            public uint ToY { get; set; }
            public uint Width { get; set; }
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

            var reader = new QuickBinaryReader(buffer);
            var imagePointer = (byte*)imageData.Scan0;

            // Moved Regions
            var movedRegions = reader.ReadUnsignedVariableLength();
            if (movedRegions != 0)
            {
                var tempBuffer = new byte[bitmap.Width * bitmap.Height * 4];

                fixed (byte* tempBufferPointer = &tempBuffer[0])
                {
                    for (var n = 0; n < movedRegions; n++)
                    {
                        var fromX = reader.ReadUnsignedVariableLength();
                        var fromY = reader.ReadUnsignedVariableLength();
                        var toX = reader.ReadUnsignedVariableLength();
                        var toY = reader.ReadUnsignedVariableLength();
                        var width = reader.ReadUnsignedVariableLength();
                        var height = reader.ReadUnsignedVariableLength();
                        var region = new MovedRegion
                        { FromX = fromX, FromY = fromY, Height = height, ToX = toX, ToY = toY, Width = width };

                        CopyRegion(imagePointer, tempBufferPointer, bitmap.Width, region);
                        CopyRegion(tempBufferPointer, imagePointer, bitmap.Width, region);
                    }
                }
            }

            // Modified Regions
            var s = 0;
            while (reader.Position < count)
            {
                var i = reader.ReadSignedVariableLength();
                var pointer = imagePointer + s + i;

                var bg = reader.ReadByte();
                var gr = reader.ReadByte();
                pointer[0] = (byte)(bg & 248);
                pointer[1] = (byte)((bg & 7) << 5 | (gr & 7) << 2);
                pointer[2] = (byte)(gr & 248);

                s += i;
            }

            bitmap.UnlockBits(imageData);
        }
    }
}