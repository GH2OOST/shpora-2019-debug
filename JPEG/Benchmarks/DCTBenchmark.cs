using System;
using BenchmarkDotNet.Attributes;
using JPEG.Benchmarks.OldImplementations;

namespace JPEG.Benchmarks
{
    [MemoryDiagnoser]
    public class DCTBenchmark
    {
        private readonly double[,] input = new double[8,8];

        [GlobalSetup]
        public void Setup()
        {
            var rnd = new Random();
            for (var y = 0; y < input.GetLength(0); y++)
                for (var x = 0; x < input.GetLength(1); x++)
                    input[y, x] = rnd.NextDouble();
        }

        [Benchmark]
        public void CurrentDCT()
        {
            var output = new double[8, 8];
            for (var i = 0; i < 64; i++)
                DCT.IDCT2D(input, output);
        }

        [Benchmark]
        public void OldDCT()
        {
            var output = new double[8, 8];
            for (var i = 0; i < 64; i++)
                Dct_old.IDCT2D(input, output);
        }
    }
}
