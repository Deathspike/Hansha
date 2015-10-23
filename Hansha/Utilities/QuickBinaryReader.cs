namespace Hansha
{
    public class QuickBinaryReader
    {
        private readonly byte[] _buffer;
        private int _position;

        public QuickBinaryReader(byte[] buffer)
        {
            _buffer = buffer;
        }

        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public byte ReadByte()
        {
            return _buffer[_position++];
        }

        public int ReadSignedVariableLength()
        {
            var result = 0;
            var shift = 0;
            var sign = -1;

            while (true)
            {
                var currentByte = ReadByte();
                result |= (currentByte & 127) << shift;
                shift += 7;
                sign <<= 7;
                if ((currentByte & 128) == 0) break;
            }

            if (((sign >> 1) & result) != 0)
            {
                result |= sign;
            }

            return result;
        }

        public uint ReadUnsignedVariableLength()
        {
            var result = (uint)0;
            var shift = 0;

            while (true)
            {
                var currentByte = ReadByte();
                result |= (uint)((currentByte & 127) << shift);
                shift += 7;
                if ((currentByte & 128) == 0) break;
            }

            return result;
        }
    }
}