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
        private MemoryStream _stream;
        private BinaryWriter _writer;

        #region Abstract

        // TODO: Yeah. This method runs in an unsafe context because of performance. Actually, because I wanted to avoid
        // MemoryStream and BinaryWriter. Performance can be similar by taking advantage of the JIT's clever tricks to
        // avoid array boundary checks. It shouldn't matter too much. Probably. Introduce a different kind of writer that
        // avoids the overhead of everything entirely? Can operate on a raw buffer directly... that'll be next!
        private unsafe void ProcessModifiedRegions(ScreenFrame frame)
        {
            fixed (byte* bBasePointer = &_buffer[0])
            fixed (byte* nBasePointer = &frame.NewPixels[0])
            fixed (byte* pBasePointer = &frame.PreviousPixels[0])
            {
                var position = _stream.Position;
                var widthInBytes = frame.Boundaries.Right * 4;
                
                foreach (var region in frame.ModifiedRegions)
                {
                    for (int y = region.Top, yOffset = y * widthInBytes; y < region.Bottom; y++, yOffset += widthInBytes)
                    {
                        var xOffset = region.Left * 4;
                        var iOffset = yOffset + xOffset;
                        var nPointer = nBasePointer + iOffset;
                        var pPointer = pBasePointer + iOffset;

                        for (var x = region.Left; x < region.Right; x++, iOffset += 4, nPointer += 4, pPointer += 4)
                        {
                            if (nPointer[0] == pPointer[0] && nPointer[1] == pPointer[1] && nPointer[2] == pPointer[2]) continue;
                            var bPointer = bBasePointer + position;
                            *(int*) bPointer = iOffset;
                            bPointer[4] = nPointer[0];
                            bPointer[5] = nPointer[1];
                            bPointer[6] = nPointer[2];
                            position += 7;
                        }
                    }
                }

                _stream.Position = position;
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

            // var clen = _buffer.CompressQuick(0, (int)_stream.Position, _bufferC);

            // await _protocolStream.SendAsync(_bufferC, 0, clen);

            await _protocolStream.SendAsync(_buffer, 0, (int) _stream.Position);
        }


        public async Task UpdateAsync(ScreenFrame frame)
        {
            var beginTime = DateTime.Now;

            _stream.Position = 0;

            ProcessMovedRegions(frame);
            ProcessModifiedRegions(frame);

            // var clen = _buffer.CompressQuick(0, (int)_stream.Position, _bufferC);

            Console.WriteLine("Proc in {0}ms", (DateTime.Now - beginTime).TotalMilliseconds);

            // await _protocolStream.SendAsync(_bufferC, 0, clen);

            await _protocolStream.SendAsync(_buffer, 0, (int)_stream.Position);

            Console.WriteLine("Sent in {0}ms", (DateTime.Now - beginTime).TotalMilliseconds);
        }

        #endregion
    }
}