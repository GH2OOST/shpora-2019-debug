using System;

namespace JPEG
{
    public static class Quantizers
    {
        private static int[,] quantizationMatrix;
        private static int currentQuality = -1;

        public static void Quantize(this double[,] channelFreqs, byte[,] output)
        {
            for (var y = 0; y < channelFreqs.GetLength(0); y++)
            for (var x = 0; x < channelFreqs.GetLength(1); x++)
                output[y, x] = (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);
        }

        public static void DeQuantize(this byte[,] quantizedBytes, double[,] output)
        {
            for (var y = 0; y < quantizedBytes.GetLength(0); y++)
            for (var x = 0; x < quantizedBytes.GetLength(1); x++)
                output[y, x] =
                    (sbyte)quantizedBytes[y, x] *
                    quantizationMatrix[y, x]; //NOTE cast to sbyte not to loose negative numbers
        }

        public static void Init(int quality = 50)
        {
            if (quality == currentQuality) return;
            currentQuality = quality;
            quantizationMatrix = GetQuantizationMatrix(quality);
        }

        public static int[,] GetQuantizationMatrix(int quality)
        {
            if (quality < 1 || quality > 99)
                throw new ArgumentException("quality must be in [1,99] interval");

            var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

            var result = new[,]
            {
                {16, 11, 10, 16, 24, 40, 51, 61},
                {12, 12, 14, 19, 26, 58, 60, 55},
                {14, 13, 16, 24, 40, 57, 69, 56},
                {14, 17, 22, 29, 51, 87, 80, 62},
                {18, 22, 37, 56, 68, 109, 103, 77},
                {24, 35, 55, 64, 81, 104, 113, 92},
                {49, 64, 78, 87, 103, 121, 120, 101},
                {72, 92, 95, 98, 112, 100, 103, 99}
            };

            for (var y = 0; y < result.GetLength(0); y++)
            for (var x = 0; x < result.GetLength(1); x++)
                result[y, x] = (multiplier * result[y, x] + 50) / 100;

            return result;
        }
    }
}