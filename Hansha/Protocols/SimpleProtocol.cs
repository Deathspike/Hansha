using System;
using System.IO;
using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    // TODO: Consider a protocol implementation based on acknowledgements. The current protocols just force a certain rate down the stream.
    public class SimpleProtocol : IProtocol
    {
        private readonly IProtocolStream _protocolStream;
        private byte[] _buffer;
        private byte[] _bufferC;
        private MemoryStream _stream;
        private BinaryWriter _writer;

        #region Abstract

        private void ProcessModifiedRegions(ScreenFrame frame)
        {
            var n = frame.NewPixels;
            var p = frame.PreviousPixels;

            foreach (var region in frame.ModifiedRegions)
            {
                var widthInBytes = frame.Boundaries.Right * 4;

                for (int y = region.Top, yOffset = y * widthInBytes; y < region.Bottom; y++, yOffset += widthInBytes)
                {
                    for (int x = region.Left, xOffset = x * 4, i = yOffset + xOffset; x < region.Right; x++, i += 4)
                    {
                        if (n[i] == p[i] && n[i + 1] == p[i + 1] && n[i + 2] == p[i + 2]) continue;
                        _writer.Write(i);
                        _writer.Write(n[i]);
                        _writer.Write(n[i + 1]);
                        _writer.Write(n[i + 2]);
                    }
                }
            }
        }

        private void ProcessMovedRegions(ScreenFrame frame)
        {
            _writer.Write(frame.MovedRegions.Length);

            foreach (var region in frame.MovedRegions)
            {
                _writer.Write(region.X);
                _writer.Write(region.Y);
                _writer.Write(region.Destination.Left);
                _writer.Write(region.Destination.Top);
                _writer.Write(region.Destination.Right - region.Destination.Left);
                _writer.Write(region.Destination.Bottom - region.Destination.Top);
            }
        }

        #endregion

        #region Constructor

        public SimpleProtocol(IProtocolStream protocolStream)
        {
            _protocolStream = protocolStream;
        }

        #endregion

        #region Implementation of IProtocol

        public async Task StartAsync(ScreenFrame frame)
        {
            _buffer = new byte[frame.Boundaries.Bottom * frame.Boundaries.Right * 7];
            _bufferC = new byte[_buffer.Length];
            _stream = new MemoryStream(_buffer);
            _writer = new BinaryWriter(_stream);

            _writer.Write(frame.Boundaries.Right);
            _writer.Write(frame.Boundaries.Bottom);

            for (var i = 0; i < frame.NewPixels.Length; i += 4)
            {
                _writer.Write(frame.NewPixels[i]);
                _writer.Write(frame.NewPixels[i + 1]);
                _writer.Write(frame.NewPixels[i + 2]);
            }

            var clen = _buffer.CompressQuick(0, (int)_stream.Position, _bufferC);

            await _protocolStream.SendAsync(_bufferC, 0, clen);
        }


        public async Task UpdateAsync(ScreenFrame frame)
        {
            var beginTime = DateTime.Now;

            _stream.Position = 0;

            ProcessMovedRegions(frame);
            ProcessModifiedRegions(frame);

            var clen = _buffer.CompressQuick(0, (int)_stream.Position, _bufferC);

            Console.WriteLine("Frame in {0}ms", (DateTime.Now - beginTime).TotalMilliseconds);

            await _protocolStream.SendAsync(_bufferC, 0, clen);
        }

        #endregion
    }
}