namespace JPEG.Images
{
    public struct Pixel
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public byte Y { get; }
        public byte Cb { get; }
        public byte Cr { get; }

        public Pixel(double firstComponent, double secondComponent, double thirdComponent, PixelFormat pixelFormat)
        {
            if (pixelFormat == PixelFormat.RGB)
            {
                
                R = ToByte(firstComponent);
                G = ToByte(secondComponent);
                B = ToByte(thirdComponent);
                Y = ToByte(16.0 + (65.738 * firstComponent + 129.057 * secondComponent + 24.064 * thirdComponent) / 256.0);
                Cb = ToByte(128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0);
                Cr = ToByte(128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0);
                return;
            }
            Y = ToByte(firstComponent);
            Cb = ToByte(secondComponent);
            Cr = ToByte(thirdComponent);
            R = ToByte((298.082 * Y + 408.583 * Cr) / 256.0 - 222.921);
            G = ToByte((298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576);
            B = ToByte((298.082 * Y + 516.412 * Cb) / 256.0 - 276.836);
        }

        private static byte ToByte(double d)
        {
            var val = (int)d;
            if (val > byte.MaxValue)
                return byte.MaxValue;
            return val < byte.MinValue ?
                byte.MinValue :
                (byte)val;
        }
    }
}