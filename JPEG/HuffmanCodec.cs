using System.IO;
using System.IO.Compression;

namespace JPEG
{
	class HuffmanCodec
	{
        public static byte[] Encode(byte[] data)
        {
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(output, CompressionLevel.Optimal))
                dstream.Write(data, 0, data.Length);
            return output.ToArray();
        }

        public static byte[] Decode(byte[] encodedData)
        {
            var input = new MemoryStream(encodedData);
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
                dstream.CopyTo(output);
            return output.ToArray();
        }
    }
}