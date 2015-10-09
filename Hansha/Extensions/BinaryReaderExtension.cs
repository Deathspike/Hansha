using System.IO;

namespace Hansha
{
    public static class BinaryReaderExtension
    {
        public static int ReadSignedVariableLength(this BinaryReader binaryReader)
        {
            var currentByte = 0;
            var result = 0;
            var shift = 0;

            while (true)
            {
                currentByte = binaryReader.ReadByte();
                result |= (currentByte & 127) << shift;
                shift += 7;

                if ((currentByte & 128) == 0)
                {
                    break;
                }
            }

            if ((currentByte & 64) != 0)
            {
                result |= -(1 << shift);
            }

            return result;
        }

        public static uint ReadUnsignedVariableLength(this BinaryReader binaryReader)
        {
            var result = (uint)0;
            var shift = 0;

            while (true)
            {
                var currentByte = binaryReader.ReadByte();
                result |= (uint)((currentByte & 127) << shift);
                shift += 7;

                if ((currentByte & 128) == 0)
                {
                    break;
                }
            }

            return result;
        }
    }
}
