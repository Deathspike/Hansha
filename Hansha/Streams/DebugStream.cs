using System;
using System.Threading.Tasks;

namespace Hansha
{
    public class DebugStream : IProtocolStream
    {
        private readonly IProtocolStream _protocolStream;
        private int _numberOfBytes;
        private DateTime? _reportTime;

        #region Constructor

        public DebugStream(IProtocolStream protocolStream)
        {
            _protocolStream = protocolStream;
        }

        #endregion

        #region Implementation of IProtocolStream

        public Task CloseAsync()
        {
            return _protocolStream.CloseAsync();
        }

        public Task<byte[]> ReceiveAsync()
        {
            return _protocolStream.ReceiveAsync();
        }

        public Task SendAsync(byte[] bytes)
        {
            if (_reportTime == null || _reportTime <= DateTime.Now)
            {
                Console.WriteLine("{0}KB/s", _numberOfBytes / 1024);
                _numberOfBytes = 0;
                _reportTime = DateTime.Now.AddSeconds(1);
            }

            _numberOfBytes += bytes.Length;

            return _protocolStream.SendAsync(bytes);
        }

        #endregion
    }
}
