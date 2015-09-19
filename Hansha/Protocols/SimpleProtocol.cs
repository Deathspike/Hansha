using System.IO;
using System.Text;
using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    // TODO: `SimpleProtocol` should support moving regions and dirty regions, too.
    // TODO: Consider a protocol implementation based on acknowledgements. The current protocols just force a certain rate down the stream.
    public class SimpleProtocol : IProtocol
    {
        private readonly IProtocolStream _protocolStream;

        #region Constructor

        public SimpleProtocol(IProtocolStream protocolStream)
        {
            _protocolStream = protocolStream;
        }

        #endregion

        #region Implementation of IProtocol
        
        public async Task StartAsync(ScreenFrame frame)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                {
                    writer.Write((ushort) frame.Boundaries.Right);
                    writer.Write((ushort) frame.Boundaries.Bottom);

                    for (var i = 0; i < frame.NewPixels.Length; i += 4)
                    {
                        writer.Write(frame.NewPixels[i]);
                        writer.Write(frame.NewPixels[i + 1]);
                        writer.Write(frame.NewPixels[i + 2]);
                    }
                }

                await _protocolStream.SendAsync(memoryStream.ToArray());
            }
        }

        public async Task UpdateAsync(ScreenFrame frame)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                {
                    for (var i = 0; i < frame.NewPixels.Length; i += 4)
                    {
                        if (frame.NewPixels[i] == frame.PreviousPixels[i] &&
                            frame.NewPixels[i + 1] == frame.PreviousPixels[i + 1] &&
                            frame.NewPixels[i + 2] == frame.PreviousPixels[i + 2]) continue;

                        writer.Write((uint)i);
                        writer.Write(frame.NewPixels[i]);
                        writer.Write(frame.NewPixels[i + 1]);
                        writer.Write(frame.NewPixels[i + 2]);
                    }
                }

                await _protocolStream.SendAsync(memoryStream.ToArray());
            }
        }

        #endregion
    }
}