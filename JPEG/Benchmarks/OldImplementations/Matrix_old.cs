using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPEG.Images;

namespace JPEG.Benchmarks.OldImplementations
{
    class Matrix_old
    {
        public readonly Pixel_old[,] Pixels;
        public readonly int Height;
        public readonly int Width;

        public Matrix_old(int height, int width)
        {
            Height = height;
            Width = width;

            Pixels = new Pixel_old[height, width];
            for (var i = 0; i < height; ++i)
            for (var j = 0; j < width; ++j)
                Pixels[i, j] = new Pixel_old(0, 0, 0, PixelFormat_old.RGB);
        }

        public static explicit operator Matrix_old(Bitmap bmp)
        {
            var height = bmp.Height - bmp.Height % 8;
            var width = bmp.Width - bmp.Width % 8;
            var matrix = new Matrix_old(height, width);

            for (var j = 0; j < height; j++)
            {
                for (var i = 0; i < width; i++)
                {
                    var pixel = bmp.GetPixel(i, j);
                    matrix.Pixels[j, i] = new Pixel_old(pixel.R, pixel.G, pixel.B, PixelFormat_old.RGB);
                }
            }

            return matrix;
        }

        public static explicit operator Bitmap(Matrix_old matrix)
        {
            var bmp = new Bitmap(matrix.Width, matrix.Height);

            for (var j = 0; j < bmp.Height; j++)
            {
                for (var i = 0; i < bmp.Width; i++)
                {
                    var pixel = matrix.Pixels[j, i];
                    bmp.SetPixel(i, j, Color.FromArgb(ToByte(pixel.R), ToByte(pixel.G), ToByte(pixel.B)));
                }
            }

            return bmp;
        }

        public static int ToByte(double d)
        {
            var val = (int)d;
            if (val > byte.MaxValue)
                return byte.MaxValue;
            if (val < byte.MinValue)
                return byte.MinValue;
            return val;
        }
    }

    class Pixel_old
    {
        private readonly PixelFormat_old format;

        public Pixel_old(double firstComponent, double secondComponent, double thirdComponent, PixelFormat_old pixelFormat)
        {
            if (!new[] { PixelFormat_old.RGB, PixelFormat_old.YCbCr }.Contains(pixelFormat))
                throw new FormatException("Unknown pixel format: " + pixelFormat);
            format = pixelFormat;
            if (pixelFormat == PixelFormat_old.RGB)
            {
                r = firstComponent;
                g = secondComponent;
                b = thirdComponent;
            }
            if (pixelFormat == PixelFormat_old.YCbCr)
            {
                y = firstComponent;
                cb = secondComponent;
                cr = thirdComponent;
            }
        }

        private readonly double r;
        private readonly double g;
        private readonly double b;

        private readonly double y;
        private readonly double cb;
        private readonly double cr;

        public double R => format == PixelFormat_old.RGB ? r : (298.082 * y + 408.583 * Cr) / 256.0 - 222.921;
        public double G => format == PixelFormat_old.RGB ? g : (298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576;
        public double B => format == PixelFormat_old.RGB ? b : (298.082 * Y + 516.412 * Cb) / 256.0 - 276.836;

        public double Y => format == PixelFormat_old.YCbCr ? y : 16.0 + (65.738 * R + 129.057 * G + 24.064 * B) / 256.0;
        public double Cb => format == PixelFormat_old.YCbCr ? cb : 128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0;
        public double Cr => format == PixelFormat_old.YCbCr ? cr : 128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0;
    }

    class PixelFormat_old
    {
        private string Format;

        private PixelFormat_old(string format)
        {
            Format = format;
        }

        public static PixelFormat_old RGB => new PixelFormat_old(nameof(RGB));
        public static PixelFormat_old YCbCr => new PixelFormat_old(nameof(YCbCr));

        protected bool Equals(PixelFormat_old other)
        {
            return string.Equals(Format, other.Format);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PixelFormat_old)obj);
        }

        public override int GetHashCode()
        {
            return (Format != null ? Format.GetHashCode() : 0);
        }

        public static bool operator ==(PixelFormat_old a, PixelFormat_old b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PixelFormat_old a, PixelFormat_old b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return Format;
        }

        ~PixelFormat_old()
        {
            Format = null;
        }
    }
}
