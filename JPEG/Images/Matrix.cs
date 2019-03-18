﻿using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images
{
    public class Matrix
    {
        private Pixel[,] Pixels; // лучше сделать private и индексатор чтобы был нормальный доступ
        public readonly int Height;
        public readonly int Width;
        public double ShiftSize { get; set; }
        public Func<Pixel, double> SelectorFunc { get; set; }

        public Matrix(int height, int width)
        { 
            SelectorFunc = pixel => pixel.Y;
            ShiftSize = 0;
            Height = height;
            Width = width;
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
            Func<Pixel, byte> componentSelector, double[,] output, int coef)
        {
            var squareCoef = coef * coef;
            var tempSum = 0d;
            for (var y = 0; y < yLength; y += coef)
            for (var x = 0; x < xLength; x += coef)
            {
                for (var yy = 0; yy < coef; yy++)
                for (var xx = 0; xx < coef; xx++)
                    tempSum += componentSelector(this[yOffset + y + yy, xOffset + x + xx]);
                output[y / coef, x / coef] = tempSum / squareCoef;
                tempSum = 0;
            }
        }

        public void SetPixels(double[,] a, double[,] b, double[,] c, PixelFormat format, int yOffset, int xOffset)
        {
            for (var y = 0; y < a.GetLength(0); y++)
            for (var x = 0; x < a.GetLength(1); x++)
                this[yOffset + y, xOffset + x] = new Pixel(a[y, x], b[y, x], c[y, x], format);
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