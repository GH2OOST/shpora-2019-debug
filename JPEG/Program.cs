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
                    selector, subMatrix);
                CompressBlock(subMatrix, channelFreqs, quantizedFreqs, quantizedBytes, allQuantizedBytes, offset);
                offset += mSize;
            }
        }

        private static void CompressBlock(double[,] subMatrix, double[,] channelFreqs,
            byte[,] quantizedFreqs, byte[] quantizedBytes, byte[] allQuantizedBytes, int offset)
        {
            ShiftMatrixValues(subMatrix , - 128);
            DCT.DCT2D(subMatrix, channelFreqs);
            channelFreqs.Quantize(quantizedFreqs);
            ZigZagScan(quantizedFreqs, quantizedBytes);
            Buffer.BlockCopy(quantizedBytes, 0, allQuantizedBytes, offset, DCTSize * DCTSize);
        }

        private static CompressedImage Compress(Matrix matrix, int quality = 50)
        {
            var size = matrix.Height * matrix.Width;
            var allQuantizedBytes = new byte[size + 2 * size / (CbCrCompressCoef * CbCrCompressCoef)];
            var offset = 0;

            var indexes =
                new List<(int x, int y, int offset)>(matrix.Height / DCTCbCrSize + matrix.Width / DCTCbCrSize);
            for (var y = 0; y < matrix.Height; y += DCTCbCrSize)
                for (var x = 0; x < matrix.Width; x += DCTCbCrSize)
                {
                    indexes.Add((x, y, offset));
                    offset += 6 * DCTSize * DCTSize;
                }

            Quantizers.Init(quality);
            var selectorFuncs = new Func<Pixel, byte>[] { p => p.Cb, p => p.Cr };

            Parallel.ForEach(indexes, () => new CompressStructures(), (index, state, s) =>
            {
                var sf = selectorFuncs;
                CompressYCbCrBlock(matrix, index.x, index.y, s.SubMatrix, s.ChannelFreqs, s.QuantizedFreqs,
                    s.QuantizedBytes, allQuantizedBytes, sf, index.offset);
                return s;

            }, f => { });

            var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var decodeTable, out var bitsCount);

            return new CompressedImage
            {
                Quality = quality,
                CompressedBytes = compressedBytes,
                BitsCount = bitsCount,
                DecodeTable = decodeTable,
                Height = matrix.Height,
                Width = matrix.Width
            };
        }

        private static Matrix Uncompress(CompressedImage image)
        {
            const int ySize = DCTCbCrSize * DCTCbCrSize / (DCTSize * DCTSize);
			var result = new Matrix(image.Height, image.Width);
            var _y = new double[ySize][,];
            for (var i = 0; i < ySize; i++)
                _y[i] = new double[DCTSize, DCTSize];
            var cb = new double[DCTSize, DCTSize];
            var cr = new double[DCTSize, DCTSize];
            var extendedcb = new double[DCTSize, DCTSize];
            var extendedcr = new double[DCTSize, DCTSize];
            var channels = new[] { cb, cr };
            var quantizedBytes = new byte[DCTSize * DCTSize];
            Quantizers.Init(image.Quality);
            var quantizedFreqs = new byte[DCTSize, DCTSize];
            var channelFreqs = new double[DCTSize, DCTSize];

            using (var allQuantizedBytes =
				new MemoryStream(HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount)))
			{
                for (var y = 0; y < image.Height; y += DCTCbCrSize)
                for (var x = 0; x < image.Width; x += DCTCbCrSize)
                {
                    for (var i = 0; i < ySize; i++)
                    {
                        allQuantizedBytes.ReadAsync(quantizedBytes, 0, quantizedBytes.Length).Wait();
                        ZigZagUnScan(quantizedBytes, quantizedFreqs);
                        quantizedFreqs.DeQuantize(channelFreqs);
                        var currentY = _y[i];
                        DCT.IDCT2D(channelFreqs, currentY);
                        ShiftMatrixValues(currentY, 128);
                    }
                    foreach (var channel in channels)
                    {
                        allQuantizedBytes.ReadAsync(quantizedBytes, 0, quantizedBytes.Length).Wait();
                        ZigZagUnScan(quantizedBytes, quantizedFreqs);
                        quantizedFreqs.DeQuantize(channelFreqs);
                        DCT.IDCT2D(channelFreqs, channel);
                        ShiftMatrixValues(channel, 128);
                    }

                    var j = 0;
                    for (var yy = 0; yy < DCTCbCrSize / DCTSize; yy++)
                    for (var xx = 0; xx < DCTCbCrSize / DCTSize; xx++)
                    {
                        ExtendMatrix(cb, xx, yy, extendedcb);
                        ExtendMatrix(cr, xx, yy, extendedcr);
                        SetPixels(result, _y[j], extendedcb, extendedcr, PixelFormat.YCbCr, y + yy * DCTSize, x + xx *DCTSize);
                        j++;
                    }
                }
            }

			return result;
		}

        private static void ExtendMatrix(double[,] matrix, int xOffset, int yOffset, double[,] output)
        {
            var coef = DCTCbCrSize / DCTSize;
            var shiftSize = DCTSize / coef;
            xOffset *= shiftSize;
            yOffset *= shiftSize;

            for (var y = 0; y < DCTSize; y++)
            for (var x = 0; x < DCTSize; x++)
                output[y, x] = matrix[(yOffset + y) / coef, (xOffset + x) / coef];
        }

		private static void ShiftMatrixValues(double[,] subMatrix, int shiftValue)
		{
			var height = subMatrix.GetLength(0);
			var width = subMatrix.GetLength(1);
			
			for(var y = 0; y < height; y++)
			for(var x = 0; x < width; x++)
				subMatrix[y, x] += shiftValue;
		}

		private static void SetPixels(Matrix matrix, double[,] a, double[,] b, double[,] c, PixelFormat format, int yOffset, int xOffset)
		{
            for(var y = 0; y < a.GetLength(0); y++)
			for(var x = 0; x < a.GetLength(1); x++)
				matrix[yOffset + y, xOffset + x] = new Pixel(a[y, x], b[y, x], c[y, x], format);
		}

		private static void ZigZagScan(byte[,] channelFreqs, byte[] output)
		{
            output[0] = channelFreqs[0, 0]; output[1] = channelFreqs[0, 1]; output[2] = channelFreqs[1, 0]; output[3] = channelFreqs[2, 0]; output[4] = channelFreqs[1, 1]; output[5] = channelFreqs[0, 2]; output[6] = channelFreqs[0, 3]; output[7] = channelFreqs[1, 2];
            output[8] = channelFreqs[2, 1]; output[9] = channelFreqs[3, 0]; output[10] = channelFreqs[4, 0]; output[11] = channelFreqs[3, 1]; output[12] = channelFreqs[2, 2]; output[13] = channelFreqs[1, 3]; output[14] = channelFreqs[0, 4]; output[15] = channelFreqs[0, 5];
            output[16] = channelFreqs[1, 4]; output[17] = channelFreqs[2, 3]; output[18] = channelFreqs[3, 2]; output[19] = channelFreqs[4, 1]; output[20] = channelFreqs[5, 0]; output[21] = channelFreqs[6, 0]; output[22] = channelFreqs[5, 1]; output[23] = channelFreqs[4, 2];
            output[24] = channelFreqs[3, 3]; output[25] = channelFreqs[2, 4]; output[26] = channelFreqs[1, 5]; output[27] = channelFreqs[0, 6]; output[28] = channelFreqs[0, 7]; output[29] = channelFreqs[1, 6]; output[30] = channelFreqs[2, 5]; output[31] = channelFreqs[3, 4];
            output[32] = channelFreqs[4, 3]; output[33] = channelFreqs[5, 2]; output[34] = channelFreqs[6, 1]; output[35] = channelFreqs[7, 0]; output[36] = channelFreqs[7, 1]; output[37] = channelFreqs[6, 2]; output[38] = channelFreqs[5, 3]; output[39] = channelFreqs[4, 4];
            output[40] = channelFreqs[3, 5]; output[41] = channelFreqs[2, 6]; output[42] = channelFreqs[1, 7]; output[43] = channelFreqs[2, 7]; output[44] = channelFreqs[3, 6]; output[45] = channelFreqs[4, 5]; output[46] = channelFreqs[5, 4]; output[47] = channelFreqs[6, 3];
            output[48] = channelFreqs[7, 2]; output[49] = channelFreqs[7, 3]; output[50] = channelFreqs[6, 4]; output[51] = channelFreqs[5, 5]; output[52] = channelFreqs[4, 6]; output[53] = channelFreqs[3, 7]; output[54] = channelFreqs[4, 7]; output[55] = channelFreqs[5, 6];
            output[56] = channelFreqs[6, 5]; output[57] = channelFreqs[7, 4]; output[58] = channelFreqs[7, 5]; output[59] = channelFreqs[6, 6]; output[60] = channelFreqs[5, 7]; output[61] = channelFreqs[6, 7]; output[62] = channelFreqs[7, 6]; output[63] = channelFreqs[7, 7];

        }

		private static void ZigZagUnScan(byte[] quantizedBytes, byte[,] output)
        {
            output[0, 0] = quantizedBytes[0]; output[0, 1] = quantizedBytes[1]; output[0, 2] = quantizedBytes[5]; output[0, 3] = quantizedBytes[6]; output[0, 4] = quantizedBytes[14]; output[0, 5] = quantizedBytes[15]; output[0, 6] = quantizedBytes[27]; output[0, 7] = quantizedBytes[28];
            output[1, 0] = quantizedBytes[2]; output[1, 1] = quantizedBytes[4]; output[1, 2] = quantizedBytes[7]; output[1, 3] = quantizedBytes[13]; output[1, 4] = quantizedBytes[16]; output[1, 5] = quantizedBytes[26]; output[1, 6] = quantizedBytes[29]; output[1, 7] = quantizedBytes[42];
            output[2, 0] = quantizedBytes[3]; output[2, 1] = quantizedBytes[8]; output[2, 2] = quantizedBytes[12]; output[2, 3] = quantizedBytes[17]; output[2, 4] = quantizedBytes[25]; output[2, 5] = quantizedBytes[30]; output[2, 6] = quantizedBytes[41]; output[2, 7] = quantizedBytes[43];
            output[3, 0] = quantizedBytes[9]; output[3, 1] = quantizedBytes[11]; output[3, 2] = quantizedBytes[18]; output[3, 3] = quantizedBytes[24]; output[3, 4] = quantizedBytes[31]; output[3, 5] = quantizedBytes[40]; output[3, 6] = quantizedBytes[44]; output[3, 7] = quantizedBytes[53];
            output[4, 0] = quantizedBytes[10]; output[4, 1] = quantizedBytes[19]; output[4, 2] = quantizedBytes[23]; output[4, 3] = quantizedBytes[32]; output[4, 4] = quantizedBytes[39]; output[4, 5] = quantizedBytes[45]; output[4, 6] = quantizedBytes[52]; output[4, 7] = quantizedBytes[54];
            output[5, 0] = quantizedBytes[20]; output[5, 1] = quantizedBytes[22]; output[5, 2] = quantizedBytes[33]; output[5, 3] = quantizedBytes[38]; output[5, 4] = quantizedBytes[46]; output[5, 5] = quantizedBytes[51]; output[5, 6] = quantizedBytes[55]; output[5, 7] = quantizedBytes[60];
            output[6, 0] = quantizedBytes[21]; output[6, 1] = quantizedBytes[34]; output[6, 2] = quantizedBytes[37]; output[6, 3] = quantizedBytes[47]; output[6, 4] = quantizedBytes[50]; output[6, 5] = quantizedBytes[56]; output[6, 6] = quantizedBytes[59]; output[6, 7] = quantizedBytes[61];
            output[7, 0] = quantizedBytes[35]; output[7, 1] = quantizedBytes[36]; output[7, 2] = quantizedBytes[48]; output[7, 3] = quantizedBytes[49]; output[7, 4] = quantizedBytes[57]; output[7, 5] = quantizedBytes[58]; output[7, 6] = quantizedBytes[62]; output[7, 7] = quantizedBytes[63];
        }
	}
}
