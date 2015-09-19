using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Hansha
{
    public static class ByteArrayExtension
    {
        public static byte[] Compress(this byte[] byteArray)
        {
            byte[] output;

            using (var ms = new MemoryStream())
            {
                using (var gs = new DeflateStream(ms, CompressionMode.Compress))
                {
                    gs.Write(byteArray, 0, byteArray.Length);
                    gs.Close();
                    output = ms.ToArray();
                }

                ms.Close();
            }

            return output;
        }

        public static byte[] Decompress(this byte[] byteArray)
        {
            var output = new List<byte>();

            using (var ms = new MemoryStream(byteArray))
            {
                using (var gs = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    var readByte = gs.ReadByte();

                    while (readByte != -1)
                    {
                        output.Add((byte)readByte);
                        readByte = gs.ReadByte();
                    }

                    gs.Close();
                }
                ms.Close();
            }

            return output.ToArray();
        }
    }
}