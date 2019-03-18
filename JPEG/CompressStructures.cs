namespace JPEG
{
    public class CompressStructures
    {
        public byte[] QuantizedBytes;
        public double[,] ChannelFreqs;
        public double[,] SubMatrix;
        public byte[,] QuantizedFreqs;

        public CompressStructures()
        {
            ChannelFreqs = new double[Program.DCTSize, Program.DCTSize];
            SubMatrix = new double[Program.DCTSize, Program.DCTSize];
            QuantizedFreqs = new byte[Program.DCTSize, Program.DCTSize];
            QuantizedBytes = new byte[Program.DCTSize * Program.DCTSize];
        }
    }
}