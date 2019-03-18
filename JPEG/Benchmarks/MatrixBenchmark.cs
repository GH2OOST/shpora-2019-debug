using System.Drawing;
using BenchmarkDotNet.Attributes;
using JPEG.Benchmarks.OldImplementations;
using JPEG.Images;

namespace JPEG.Benchmarks
{
    [MemoryDiagnoser]
    public class MatrixBenchmark
    {
        private Bitmap bmp = new Bitmap(100, 100);

        [Benchmark]
        public void CurrentMatrix()
        {
            var a = (Matrix) bmp;
        }

        [Benchmark]
        public void OldMatrix()
        {
            var a = (Matrix_old)bmp;
        }
    }
}