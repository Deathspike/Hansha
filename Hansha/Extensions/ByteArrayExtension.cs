using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Hansha
{
    public static class ByteArrayExtension
    {
        public static int CompressQuick(this byte[] uncompressed, int offset, int count, byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            {
                using (var deflateStream = new GZipStream(memoryStream, CompressionLevel.Fastest, true))
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
                using (var deflateStream = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress, true))
                {
                    deflateStream.CopyTo(uncompressedMemoryStream);
                }

                return (int)uncompressedMemoryStream.Position;
            }
        }
    }
}