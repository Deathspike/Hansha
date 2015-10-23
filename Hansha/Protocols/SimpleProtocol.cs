using System;
using System.Threading.Tasks;
using Hansha.Core;

namespace Hansha
{
    // TODO: Consider double buffering.
    // TODO: Consider a protocol implementation based on acknowledgements. The current protocols just force a certain rate down the stream.
    public class SimpleProtocol : IProtocol
    {
        private readonly IProtocolStream _protocolStream;
        private byte[] _buffer;
        private byte[] _bufferCompressed;
        private QuickBinaryWriter _writer;

        #region Abstract

        private void ProcessModifiedRegions(ScreenFrame frame)
        {
            var n = frame.NewPixels;
            var p = frame.PreviousPixels;
            var s = 0;
            var w = frame.Boundaries.Right * 4;

            foreach (var region in frame.ModifiedRegions)
            {
                for (int y = region.Top, yOffset = y * w; y < region.Bottom; y++, yOffset += w)
                {
                    for (int x = region.Left, xOffset = x * 4, i = yOffset + xOffset; x < region.Right; x++, i += 4)
                    {
                        if (n[i] == p[i] && n[i + 1] == p[i + 1] && n[i + 2] == p[i + 2]) continue;
                        _writer.WriteSignedVariableLength(i - s);
                        _writer.WriteByte((byte)((n[i + 0] & 248) | (n[i + 1] & 224) >> 5));
                        _writer.WriteByte((byte)((n[i + 2] & 248) | (n[i + 1] & 28) >> 2));
                        s = i;
                    }
                }
            }
        }

        private void ProcessMovedRegions(ScreenFrame frame)
        {
            _writer.WriteUnsignedVariableLength((uint)frame.MovedRegions.Length);

            foreach (var region in frame.MovedRegions)
            {
                _writer.WriteUnsignedVariableLength((uint)region.X);
                _writer.WriteUnsignedVariableLength((uint)region.Y);
                _writer.WriteUnsignedVariableLength((uint)region.Destination.Left);
                _writer.WriteUnsignedVariableLength((uint)region.Destination.Top);
                _writer.WriteUnsignedVariableLength((uint)(region.Destination.Right - region.Destination.Left));
                _writer.WriteUnsignedVariableLength((uint)(region.Destination.Bottom - region.Destination.Top));
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
            _bufferCompressed = new byte[frame.Boundaries.Bottom * frame.Boundaries.Right * 7];
            _writer = new QuickBinaryWriter(_buffer);

            _writer.WriteUnsignedVariableLength((uint)frame.Boundaries.Right);
            _writer.WriteUnsignedVariableLength((uint)frame.Boundaries.Bottom);

            for (var i = 0; i < frame.NewPixels.Length; i += 4)
            {
                _writer.WriteByte((byte)((frame.NewPixels[i + 0] & 248) | (frame.NewPixels[i + 1] & 224) >> 5));
                _writer.WriteByte((byte)((frame.NewPixels[i + 2] & 248) | (frame.NewPixels[i + 1] & 28) >> 2));
            }

            var len = _buffer.CompressQuick(0, _writer.Position, _bufferCompressed);

            await _protocolStream.SendAsync(_bufferCompressed, 0, len);

            // await _protocolStream.SendAsync(_buffer, 0, _writer.Position);
        }


        public async Task UpdateAsync(ScreenFrame frame)
        {
            var beginTime = DateTime.Now;
            _writer.Position = 0;

            ProcessMovedRegions(frame);
            ProcessModifiedRegions(frame);

            var len = _buffer.CompressQuick(0, _writer.Position, _bufferCompressed);

            if ((DateTime.Now - beginTime).TotalMilliseconds > 30)
            {
                Console.WriteLine("Proc in {0}ms", (DateTime.Now - beginTime).TotalMilliseconds);
            }

            await _protocolStream.SendAsync(_bufferCompressed, 0, len);

            // await _protocolStream.SendAsync(_buffer, 0, _writer.Position);

            if ((DateTime.Now - beginTime).TotalMilliseconds > 30)
            {
                Console.WriteLine("Sent in {0}ms", (DateTime.Now - beginTime).TotalMilliseconds);
            }
        }

        #endregion
    }
}