namespace JPEG
{
    class UncompressStructures
    {
        public double[][,] YChannel;
        public double[][,] CbcrChannels;
        public double[][,] extendedcbcrChannels;
        public byte[] QuantizedBytes;
        public byte[,] QuantizedFreqs;
        public double[,] ChannelFreqs;

        public UncompressStructures(int ySize, int DCTSize)
        {
            YChannel = new double[ySize][,];
            for (var i = 0; i < ySize; i++)
                YChannel[i] = new double[DCTSize, DCTSize];
            var cb = new double[DCTSize, DCTSize];
            var cr = new double[DCTSize, DCTSize];
            CbcrChannels = new[] { cb, cr };
            var extendedcb = new double[DCTSize, DCTSize];
            var extendedcr = new double[DCTSize, DCTSize];
            extendedcbcrChannels = new[] { extendedcb, extendedcr };
            QuantizedBytes = new byte[DCTSize * DCTSize];
            QuantizedFreqs = new byte[DCTSize, DCTSize];
            ChannelFreqs = new double[DCTSize, DCTSize];
        }
    }
}