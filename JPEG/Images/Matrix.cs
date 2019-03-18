using System;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JPEG.Images
{
    public class Matrix
    {
        private const int Coef = Program.DCTCbCrSize / Program.DCTSize;
        private Pixel[,] Pixels; // лучше сделать private и индексатор чтобы был нормальный доступ
        public readonly int Height;
        public readonly int Width;
        public double ShiftSize { get; set; }
        //private readonly int yOffset;
        //private readonly int xOffset;
        public Func<Pixel, double> SelectorFunc { get; set; }

        public Matrix(int height, int width)
        {
            //Matrix(height, width, 0, 0, new Pixel[height, width], 0);
            SelectorFunc = pixel => pixel.Y;
            ShiftSize = 0;
            Height = height;
            Width = width;
            //yOffset = 0;
            //xOffset = 0;
            Pixels = new Pixel[height, width];
        }

        public Pixel this[int y, int x]
        {
            get => Pixels[y, x];

            set => Pixels[y, x] = value;
        }

        public void GetSubMatrix(int yOffset, int yLength, int xOffset, int xLength,
            Func<Pixel, byte> componentSelector, double[,] output)
        {
            for (var y = 0; y < yLength; y++)
            for (var x = 0; x < xLength; x++)
                output[y, x] = componentSelector(this[yOffset + y, xOffset + x]);
        }

        public void GetSubMatrixAndCompress(int yOffset, int yLength, int xOffset, int xLength,
            Func<Pixel, byte> componentSelector, double[,] output)
        {
            var tempSum = 0d;
            for (var y = 0; y < yLength; y += Coef)
            for (var x = 0; x < xLength; x += Coef)
            {
                for (var yy = 0; yy < Coef; yy++)
                for (var xx = 0; xx < Coef; xx++)
                    tempSum += componentSelector(this[yOffset + y + yy, xOffset + x + xx]);
                output[y / Coef, x / Coef] = tempSum / (Coef * Coef);
                tempSum = 0;
            }
        }

        private static unsafe void GetPixels(Bitmap b, Matrix matrix)
        {
            var bData = b.LockBits(new Rectangle(0, 0, matrix.Width, matrix.Height), ImageLockMode.ReadWrite, b.PixelFormat);

            var scan0 = (byte*)bData.Scan0.ToPointer();

            for (var y = 0; y < bData.Height; ++y)
            for (var x = 0; x < bData.Width; ++x)
            {
                var data = scan0 + y * bData.Stride + x * 3;
                matrix.Pixels[y, x] = new Pixel(data[2], data[1], data[0], PixelFormat.RGB);
            }

            b.UnlockBits(bData);
        }

        private static unsafe void SetPixels(Bitmap b, Matrix matrix)
        {
            var bData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, b.PixelFormat);

            var scan0 = (byte*)bData.Scan0.ToPointer();

            for (var y = 0; y < bData.Height; ++y)
            for (var x = 0; x < bData.Width; ++x)
            {
                var data = scan0 + y * bData.Stride + x * 4;
                var pixel = matrix.Pixels[y, x];
                data[0] = pixel.B;
                data[1] = pixel.G;
                data[2] = pixel.R;
            }

            b.UnlockBits(bData);
        }

        public static explicit operator Matrix(Bitmap bmp)
        {
            var height = bmp.Height - bmp.Height % Program.DCTCbCrSize;
            var width = bmp.Width - bmp.Width % Program.DCTCbCrSize;
            var matrix = new Matrix(height, width);
            GetPixels(bmp, matrix);

            return matrix;
        }

        public static explicit operator Bitmap(Matrix matrix)
        {
            var bmp = new Bitmap(matrix.Width, matrix.Height);
            SetPixels(bmp, matrix);

            return bmp;
        }
    }
}