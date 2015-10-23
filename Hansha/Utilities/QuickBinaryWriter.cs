namespace Hansha
{
    public class QuickBinaryWriter
    {
        private readonly byte[] _buffer;
        private int _position;

        public QuickBinaryWriter(byte[] buffer)
        {
            _buffer = buffer;
        }

        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public void WriteByte(byte value)
        {
            _buffer[_position++] = value;
        }

        public void WriteSignedVariableLength(int value)
        {
            while (true)
            {
                var currentByte = (byte)(value & 127);

                value >>= 7;

                if ((value == 0 && (currentByte & 64) == 0) || (value == -1 && (currentByte & 64) != 0))
                {
                    WriteByte(currentByte);
                    break;
                }

                WriteByte((byte)(currentByte | 128));
            }
        }

        public void WriteUnsignedVariableLength(uint value)
        {
            while (value >= 128)
            {
                WriteByte((byte)(value | 128));
                value >>= 7;
            }

            WriteByte((byte)value);
        }
    }
}