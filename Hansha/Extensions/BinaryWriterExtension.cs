using System.IO;

namespace Hansha
{
    public static class BinaryWriterExtensions
    {
        public static void WriteSignedVariableLength(this BinaryWriter binaryWriter, int value)
        {
            var hasMore = true;

            while (hasMore)
            {
                var currentByte = (byte) (value & 127);

                value >>= 7;

                hasMore = !((value == 0 && (currentByte & 64) == 0) || (value == -1 && (currentByte & 64) != 0));

                if (hasMore)
                {
                    currentByte |= 128;
                }

                binaryWriter.Write(currentByte);
            }
        }

        public static void WriteUnsignedVariableLength(this BinaryWriter binaryWriter, uint value)
        {
            while (value >= 128)
            {
                binaryWriter.Write((byte)(value | 128));
                value >>= 7;
            }

            binaryWriter.Write((byte)value);
        }
    }
}