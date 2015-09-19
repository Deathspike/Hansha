using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    public class HyperProtocol : IProtocol
    {
        private readonly IProtocolStream _protocolStream;

        #region Abstract

        private static void ProcessMovedRegions(BinaryWriter binaryWriter, ScreenFrame frame)
        {
            // Write the number of moved regions.
            binaryWriter.WriteUnsignedVariableLength((uint)frame.MovedRegions.Length);

            // Iterate through each moved region.
            foreach (var movedRegion in frame.MovedRegions)
            {
                binaryWriter.WriteUnsignedVariableLength((uint) movedRegion.X);
                binaryWriter.WriteUnsignedVariableLength((uint) movedRegion.Y);
                binaryWriter.WriteUnsignedVariableLength((uint) movedRegion.Destination.Left);
                binaryWriter.WriteUnsignedVariableLength((uint) movedRegion.Destination.Top);
                binaryWriter.WriteUnsignedVariableLength((uint) (movedRegion.Destination.Right - movedRegion.Destination.Left));
                binaryWriter.WriteUnsignedVariableLength((uint) (movedRegion.Destination.Bottom - movedRegion.Destination.Top));
            }
        }

        private static void ProcessModifiedRegions(BinaryWriter binaryWriter, ScreenFrame frame)
        {
            var pi = 0;

            // Iterate through each modified region.
            foreach (var modifiedRegion in frame.ModifiedRegions)
            {
                var n = frame.Boundaries.Right * 4;

                // Iterate from top to bottom.
                for (int y = modifiedRegion.Top, yi = y * n; y < modifiedRegion.Bottom; y++, yi += n)
                {
                    // Iterate from left to right.
                    for (int x = modifiedRegion.Left, ci = yi + x * 4; x < modifiedRegion.Right; x++, ci += 4)
                    {
                        // Check if the pixel has not been changed.
                        if (frame.NewPixels[ci + 0] == frame.PreviousPixels[ci + 0] &&
                            frame.NewPixels[ci + 1] == frame.PreviousPixels[ci + 1] &&
                            frame.NewPixels[ci + 2] == frame.PreviousPixels[ci + 2] &&
                            frame.NewPixels[ci + 3] == frame.PreviousPixels[ci + 3]) continue;

                        // Write the index mutation, each channel, and track the index.
                        binaryWriter.WriteSignedVariableLength(ci - pi);
                        binaryWriter.Write((byte) ((frame.NewPixels[ci + 0] & 248) | (frame.NewPixels[ci + 1] & 224) >> 5));
                        binaryWriter.Write((byte) ((frame.NewPixels[ci + 2] & 248) | (frame.NewPixels[ci + 1] & 28) >> 2));
                        pi = ci;
                    }
                }
            }
        }

        #endregion

        #region Constructor

        public HyperProtocol(IProtocolStream protocolStream)
        {
            _protocolStream = protocolStream;
        }

        #endregion

        #region Implementation of IProtocol
        
        public async Task StartAsync(ScreenFrame frame)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
                using (var binaryWriter = new BinaryWriter(memoryStream, Encoding.Default, true))
                {
                    binaryWriter.Write((ushort)frame.Boundaries.Right);
                    binaryWriter.Write((ushort)frame.Boundaries.Bottom);

                    for (var i = 0; i < frame.NewPixels.Length; i += 4)
                    {
                        binaryWriter.Write((byte) ((frame.NewPixels[i + 0] & 248) | (frame.NewPixels[i + 1] & 224) >> 5));
                        binaryWriter.Write((byte) ((frame.NewPixels[i + 2] & 248) | (frame.NewPixels[i + 1] & 28) >> 2));
                    }
                }

                await _protocolStream.SendAsync(memoryStream.ToArray());
            }
        }

        public async Task UpdateAsync(ScreenFrame frame)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
                using (var binaryWriter = new BinaryWriter(memoryStream, Encoding.Default, true))
                {
                    ProcessMovedRegions(binaryWriter, frame);
                    ProcessModifiedRegions(binaryWriter, frame);
                }

                await _protocolStream.SendAsync(memoryStream.ToArray());
            }
        }

        #endregion
    }
}