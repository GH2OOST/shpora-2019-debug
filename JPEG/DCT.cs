using System;

namespace JPEG
{
	public class DCT
	{
        private static readonly double OneDivideSqrt2 = 1 / Math.Sqrt(2);
        private static readonly double[,] BasisFunctionCache = new double[Program.DCTSize, Program.DCTSize];

        static DCT()
        {
            FillCache();
        }

        private static void FillCache()
        {
            for (var y = 0; y < BasisFunctionCache.GetLength(0); y++)
            for (var x = 0; x < BasisFunctionCache.GetLength(1); x++)
                BasisFunctionCache[y, x] = Math.Cos((2d * y + 1d) * x * Math.PI / (2 * Program.DCTSize));
        }

        public static void DCT2D(double[,] input, double[,] output)
		{
			var height = input.GetLength(0);
			var width = input.GetLength(1);
                
            var beta = Beta(height, width);

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sum = 0d;
                for (var sumY = 0; sumY < height; sumY++)
                for (var sumX = 0; sumX < width; sumX++)
                    sum += BasisFunction(input[sumY, sumX], x, y, sumX, sumY);
                output[y, x] = sum * beta * Alpha(x) * Alpha(y);
            }
		}

		public static void IDCT2D(double[,] input, double[,] output)
		{
            var height = input.GetLength(0);
            var width = input.GetLength(1);

            var beta = Beta(height, width);

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sum = 0d;
                for (var sumY = 0; sumY < height; sumY++)
                for (var sumX = 0; sumX < width; sumX++)
                    sum += BasisFunction(input[sumY, sumX], sumX, sumY, x, y) * Alpha(sumX) * Alpha(sumY);
                output[y, x] = sum * beta;
            }
        }

        public static double BasisFunction(double a, double sumX, double sumY, double x, double y)
        {
            var b = CosFunc((int) sumX, (int) x);
            var c = CosFunc((int) sumY, (int) y);

            return a * b * c;
        }

        private static double CosFunc(int u, int x) => 
            BasisFunctionCache[x, u];

        private static double Alpha(int u)
        {
            if (u == 0)
                return OneDivideSqrt2;

            return 1;
		}

		private static double Beta(int height, int width)
		{
			return 1d / width + 1d / height;
		}
	}
}