namespace JPEG
{
    public class CompressStructures
    {
        public byte[] QuantizedBytes;
        public double[,] ChannelFreqs;
        public double[,] SubMatrix;
        public byte[,] QuantizedFreqs;

        public CompressStructures(int size)
        {
            ChannelFreqs = new double[size, size];
            SubMatrix = new double[size, size];
            QuantizedFreqs = new byte[size, size];
            QuantizedBytes = new byte[size * size];
        }
    }
}