using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using JPEG.Benchmarks;
using JPEG.Images;
using PixelFormat = JPEG.Images.PixelFormat;

namespace JPEG
{
	class Program
	{
		const int CompressionQuality = 70;
        public const int DCTSize = 8;
        public const int DCTCbCrSize = 16;
        public const int CbCrCompressCoef = DCTCbCrSize / DCTSize;

        static void Main(string[] args)
		{
#if bench
            BenchmarkRunner.Run<DCTBenchmark>();
            //BenchmarkRunner.Run<MatrixBenchmark>();
            Console.ReadKey();
            return;
#endif

            try
            {
				Console.WriteLine(IntPtr.Size == 8 ? "64-bit version" : "32-bit version");
				var sw = Stopwatch.StartNew();
				var fileName = @"sample.bmp";
//				var fileName = "Big_Black_River_Railroad_Bridge.bmp";
				var compressedFileName = fileName + ".compressed." + CompressionQuality;
				var uncompressedFileName = fileName + ".uncompressed." + CompressionQuality + ".bmp";
				
				using (var fileStream = File.OpenRead(fileName))
				using (var bmp = (Bitmap) Image.FromStream(fileStream, false, false))
				{
					var imageMatrix = (Matrix) bmp;

					sw.Stop();
					Console.WriteLine($"{bmp.Width}x{bmp.Height} - {fileStream.Length / (1024.0 * 1024):F2} MB");
					sw.Start();

					var compressionResult = Compress(imageMatrix, CompressionQuality);
					compressionResult.Save(compressedFileName);
				}

				sw.Stop();
				Console.WriteLine("Compression: " + sw.Elapsed);
				sw.Restart();
				var compressedImage = CompressedImage.Load(compressedFileName);
				var uncompressedImage = Uncompress(compressedImage);
				var resultBmp = (Bitmap) uncompressedImage;
				resultBmp.Save(uncompressedFileName, ImageFormat.Bmp);
				Console.WriteLine("Decompression: " + sw.Elapsed);
				Console.WriteLine($"Peak commit size: {MemoryMeter.PeakPrivateBytes() / (1024.0*1024):F2} MB");
				Console.WriteLine($"Peak working set: {MemoryMeter.PeakWorkingSet() / (1024.0*1024):F2} MB");
                //Console.ReadKey();
            }
			catch(Exception e)
			{
				Console.WriteLine(e);
			}
        }

        private static byte ToY(Pixel p) => p.Y;

        private static void CompressYCbCrBlock(Matrix matrix, int x, int y, double[,] subMatrix, double[,] channelFreqs,
            byte[,] quantizedFreqs, byte[] quantizedBytes, byte[] allQuantizedBytes,
            Func<Pixel, byte>[] selectorFuncs, int offset)
        {
            var mSize = DCTSize * DCTSize;
            for (var yy = y; yy < y + DCTCbCrSize; yy += DCTSize)
            for (var xx = x; xx < x + DCTCbCrSize; xx += DCTSize)
            {
                matrix.GetSubMatrix(yy, DCTSize, xx, DCTSize, ToY, subMatrix);
                CompressBlock(subMatrix, channelFreqs, quantizedFreqs, quantizedBytes, allQuantizedBytes, offset);
                offset += mSize;
            }

            foreach (var selector in selectorFuncs)
            {
                matrix.GetSubMatrixAndCompress(y, DCTCbCrSize, x, DCTCbCrSize,
                    selector, subMatrix, CbCrCompressCoef);
                CompressBlock(subMatrix, channelFreqs, quantizedFreqs, quantizedBytes, allQuantizedBytes, offset);
                offset += mSize;
            }
        }

        private static void CompressBlock(double[,] subMatrix, double[,] channelFreqs,
            byte[,] quantizedFreqs, byte[] quantizedBytes, byte[] allQuantizedBytes, int offset)
        {
            subMatrix.ShiftMatrixValues(-128);
            DCT.DCT2D(subMatrix, channelFreqs);
            channelFreqs.Quantize(quantizedFreqs);
            quantizedFreqs.ZigZagScan(quantizedBytes);
            Buffer.BlockCopy(quantizedBytes, 0, allQuantizedBytes, offset, DCTSize * DCTSize);
        }

        private static CompressedImage Compress(Matrix matrix, int quality = 50)
        {
            var size = matrix.Height * matrix.Width;
            var allQuantizedBytes = new byte[size + 2 * size / (CbCrCompressCoef * CbCrCompressCoef)];

            var offset = 0;
            var indexes =
                new List<(int x, int y, int offset)>(matrix.Height / DCTCbCrSize * (matrix.Width / DCTCbCrSize));
            for (var y = 0; y < matrix.Height; y += DCTCbCrSize)
            for (var x = 0; x < matrix.Width; x += DCTCbCrSize)
            {
                indexes.Add((x, y, offset));
                offset += (2 + CbCrCompressCoef * CbCrCompressCoef) * DCTSize * DCTSize;
            }

            Quantizers.Init(quality);
            var selectorFuncs = new Func<Pixel, byte>[] { p => p.Cb, p => p.Cr };

            Parallel.ForEach(indexes, () => new CompressStructures(DCTSize), (index, state, s) =>
            {
                var sf = selectorFuncs;
                CompressYCbCrBlock(matrix, index.x, index.y, s.SubMatrix, s.ChannelFreqs, s.QuantizedFreqs,
                    s.QuantizedBytes, allQuantizedBytes, sf, index.offset);
                return s;

            }, f => { });

            var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes);

            return new CompressedImage
            {
                Quality = quality,
                CompressedBytes = compressedBytes,
                //BitsCount = bitsCount,
                //DecodeTable = decodeTable,
                Height = matrix.Height,
                Width = matrix.Width
            };
        }

        private static void UncompressBlock(byte[] allQuantizedBytes, byte[] quantizedBytes,
            byte[,] quantizedFreqs, double[,] channelFreqs, double[,] channel, int offset)
        {
            Buffer.BlockCopy(allQuantizedBytes, offset, quantizedBytes, 0, DCTSize * DCTSize);
            quantizedBytes.ZigZagUnScan(quantizedFreqs);
            quantizedFreqs.DeQuantize(channelFreqs);
            DCT.IDCT2D(channelFreqs, channel);
            channel.ShiftMatrixValues(128);
        }

        private static void UncompressYCbCrBlock(int ySize, byte[] allQuantizedBytes, byte[] quantizedBytes,
            byte[,] quantizedFreqs, double[,] channelFreqs, double[][,] yChannel, double[][,] cbcrChannels,
            double[][,] extendedcbcrChannels, Matrix result, int y, int x, int offset)
        {
            for (var i = 0; i < ySize; i++)
            {
                UncompressBlock(allQuantizedBytes, quantizedBytes, quantizedFreqs, channelFreqs, yChannel[i], offset);
                offset += DCTSize * DCTSize;
            }

            foreach (var channel in cbcrChannels)
            {
                UncompressBlock(allQuantizedBytes, quantizedBytes, quantizedFreqs, channelFreqs, channel, offset);
                offset += DCTSize * DCTSize;
            }

            var j = 0;
            for (var yy = 0; yy < DCTCbCrSize / DCTSize; yy++)
            for (var xx = 0; xx < DCTCbCrSize / DCTSize; xx++)
            {
                for (var i = 0; i < 2; i++)
                    cbcrChannels[i].ExtendMatrix(xx, yy, CbCrCompressCoef, DCTSize, extendedcbcrChannels[i]);
                result.SetPixels(yChannel[j], extendedcbcrChannels[0], extendedcbcrChannels[1], PixelFormat.YCbCr, y + yy * DCTSize, x + xx * DCTSize);
                j++;
            }
        }

        private static Matrix Uncompress(CompressedImage image)
        {
            const int ySize = DCTCbCrSize * DCTCbCrSize / (DCTSize * DCTSize);
			var result = new Matrix(image.Height, image.Width);
            
            Quantizers.Init(image.Quality);

            var offset = 0;
            var indexes =
                new List<(int x, int y, int offset)>(image.Height / DCTCbCrSize * (image.Width / DCTCbCrSize));
            for (var y = 0; y < image.Height; y += DCTCbCrSize)
            for (var x = 0; x < image.Width; x += DCTCbCrSize)
            {
                indexes.Add((x, y, offset));
                offset += (2 + CbCrCompressCoef * CbCrCompressCoef) * DCTSize * DCTSize;
            }

            var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes);

            Parallel.ForEach(indexes, () => new UncompressStructures(ySize, DCTSize), (index, state, s) =>
            {
                var ys = ySize;
                var aqb = allQuantizedBytes;
                UncompressYCbCrBlock(ys, aqb, s.QuantizedBytes, s.QuantizedFreqs, s.ChannelFreqs,
                    s.YChannel, s.CbcrChannels, s.extendedcbcrChannels, result, index.y, index.x, index.offset);
                return s;
            }, f => { });

			return result;
		}
	}
}
