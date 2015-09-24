using System;
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
        private byte[] _buffer;
        private MemoryStream _stream;
        private BinaryWriter _writer;

        #region Constructor

        public SimpleProtocol(IProtocolStream protocolStream)
        {
            _protocolStream = protocolStream;
        }

        #endregion

        #region Implementation of IProtocol
        
        public async Task StartAsync(ScreenFrame frame)
        {
            _buffer = new byte[4 + frame.Boundaries.Bottom * frame.Boundaries.Right * 7];
            _stream = new MemoryStream(_buffer);
            _writer = new BinaryWriter(_stream);

            _writer.Write((ushort)frame.Boundaries.Right);
            _writer.Write((ushort)frame.Boundaries.Bottom);

            for (var i = 0; i < frame.NewPixels.Length; i += 4)
            {
                _writer.Write(frame.NewPixels[i]);
                _writer.Write(frame.NewPixels[i + 1]);
                _writer.Write(frame.NewPixels[i + 2]);
            }

            await _protocolStream.SendAsync(_buffer, 0, (int)_stream.Position);
        }
        
        public async Task UpdateAsync(ScreenFrame frame)
        {
            var beginTime = DateTime.Now;
            var np = frame.NewPixels;
            var npLength = np.Length;
            var pp = frame.PreviousPixels;

            _stream.Position = 0;

            for (var i = 0; i < npLength; i += 4)
            {
                if (np[i] == pp[i] && np[i + 1] == pp[i + 1] && np[i + 2] == pp[i + 2]) continue;
                _writer.Write((uint)i);
                _writer.Write(np[i]);
                _writer.Write(np[i + 1]);
                _writer.Write(np[i + 2]);
            }

            Console.WriteLine("Spent {0}ms processing frame", (DateTime.Now - beginTime).Milliseconds);

            await _protocolStream.SendAsync(_buffer, 0, (int)_stream.Position);
        }

        #endregion
    }
}