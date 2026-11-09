
using System.Runtime.InteropServices;

namespace LumaCtrl.Services
{
    public sealed record ScreenStats(
        double Brightness,
        double R,
        double G,
        double B,
        double Chroma);

    public sealed class ScreenSampler : IDisposable
    {
        private const int W = 32;
        private const int H = 18;

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize, biWidth, biHeight;
            public short biPlanes, biBitCount;
            public int biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public int biClrUsed, biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(
            IntPtr hdc,
            ref BITMAPINFO pbmi,
            uint usage,
            out IntPtr ppvBits,
            IntPtr hSection,
            uint offset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(
            IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
            IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc,
            uint rop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private readonly IntPtr _screenDc;
        private readonly IntPtr _memDc;
        private readonly IntPtr _bitmap;
        private readonly IntPtr _oldBitmap;
        private readonly IntPtr _bits;

        private readonly byte[] _buffer = new byte[W * H * 4];

        public ScreenSampler()
        {
            _screenDc = GetDC(IntPtr.Zero);
            _memDc = CreateCompatibleDC(_screenDc);

            var bmi = new BITMAPINFO();

            bmi.bmiHeader.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.bmiHeader.biWidth = W;
            bmi.bmiHeader.biHeight = -H;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;

            _bitmap = CreateDIBSection(
                _memDc,
                ref bmi,
                0,
                out _bits,
                IntPtr.Zero,
                0);

            _oldBitmap = SelectObject(_memDc, _bitmap);

            SetStretchBltMode(_memDc, 4);
        }

        public ScreenStats Sample()
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            if (!StretchBlt(
                _memDc, 0, 0, W, H,
                _screenDc, 0, 0, screenWidth, screenHeight,
                0x00CC0020))
            {
                throw new Exception("Screen capture failed.");
            }

            Marshal.Copy(_bits, _buffer, 0, _buffer.Length);

            double sumV = 0;
            double sumW = 0;
            double wr = 0;
            double wg = 0;
            double wb = 0;

            for (int i = 0; i < _buffer.Length; i += 4)
            {
                double b = _buffer[i];
                double g = _buffer[i + 1];
                double r = _buffer[i + 2];

                double max = Math.Max(r, Math.Max(g, b));
                double min = Math.Min(r, Math.Min(g, b));

                if (max < 1)
                    continue;

                double v = max / 255.0;
                double sat = (max - min) / max;

                sumV += v;

                double weight = v * sat * sat;

                wr += r / max * weight;
                wg += g / max * weight;
                wb += b / max * weight;

                sumW += weight;
            }

            int pixelCount = W * H;

            double brightness = sumV / pixelCount;
            double chroma = sumW / pixelCount;

            if (sumW < 1e-6)
            {
                return new ScreenStats(
                    brightness, 255, 255, 255, 0);
            }

            return new ScreenStats(
                brightness,
                wr / sumW * 255,
                wg / sumW * 255,
                wb / sumW * 255,
                chroma);
        }

        public void Dispose()
        {
            SelectObject(_memDc, _oldBitmap);
            DeleteObject(_bitmap);
            DeleteDC(_memDc);
            ReleaseDC(IntPtr.Zero, _screenDc);
        }
    }
}
