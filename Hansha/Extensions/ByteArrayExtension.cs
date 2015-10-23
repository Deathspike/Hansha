using lz4;
using System.IO;

namespace Hansha
{
    public static class ByteArrayExtension
    {
        public static int CompressQuick(this byte[] uncompressed, int offset, int count, byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            {
                using (var deflateStream = LZ4Stream.CreateCompressor(memoryStream, LZ4StreamMode.Write, leaveInnerStreamOpen: true))
                {
                    deflateStream.Write(uncompressed, offset, count);
                }

                return (int) memoryStream.Position;
            }
        }

        public static int DecompressQuick(this byte[] compressed, byte[] uncompressed)
        {
            using (var uncompressedMemoryStream = new MemoryStream(uncompressed))
            {
                using (var deflateStream = LZ4Stream.CreateDecompressor(new MemoryStream(compressed), LZ4StreamMode.Read, true))
                {
                    deflateStream.CopyTo(uncompressedMemoryStream);
                }

                return (int)uncompressedMemoryStream.Position;
            }
        }
    }
}