
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LumaCtrl.Views
{
    public partial class ColorPickerControl : UserControl
    {
        private const int WheelSize = 250;

        private bool _isUpdating;
        private bool _isDragging;

        public event Action<Color>? ColorChanged;

        public ColorPickerControl()
        {
            InitializeComponent();

            RenderColorWheel();

            SetWheelPosition(0, 1);

            UpdateHexInput(
                GetColorFromHsv(0, 1, 1));
        }

        private void RenderColorWheel()
        {
            int size = WheelSize;
            int stride = size * 4;

            byte[] pixels = new byte[stride * size];

            double center = size / 2.0;
            double radius = center - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - center;
                    double dy = y - center;

                    double distance = Math.Sqrt(
                        dx * dx + dy * dy);

                    int index = y * stride + x * 4;

                    if (distance > radius)
                    {
                        pixels[index + 3] = 0;
                        continue;
                    }

                    double angle = Math.Atan2(dy, dx)
                        * 180.0 / Math.PI;

                    if (angle < 0)
                        angle += 360;

                    double saturation = distance / radius;

                    Color color = GetColorFromHsv(
                        angle,
                        saturation,
                        1);

                    // WriteableBitmap uses BGRA pixel order.
                    pixels[index] = color.B;
                    pixels[index + 1] = color.G;
                    pixels[index + 2] = color.R;
                    pixels[index + 3] = 255;
                }
            }

            var bitmap = new WriteableBitmap(
                size,
                size,
                96,
                96,
                PixelFormats.Bgra32,
                null);

            bitmap.WritePixels(
                new Int32Rect(0, 0, size, size),
                pixels,
                stride,
                0);

            ColorWheelImage.Source = bitmap;
        }

        private void Wheel_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            _isDragging = true;

            WheelCanvas.CaptureMouse();

            UpdateFromPosition(e.GetPosition(WheelCanvas));
        }

        private void Wheel_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            UpdateFromPosition(e.GetPosition(WheelCanvas));
        }

        private void Wheel_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            _isDragging = false;

            WheelCanvas.ReleaseMouseCapture();
        }

        private void Wheel_LostMouseCapture(
            object sender,
            MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void UpdateFromPosition(Point position)
        {
            double center = WheelSize / 2.0;
            double radius = center - 1;

            double dx = position.X - center;
            double dy = position.Y - center;

            double distance = Math.Sqrt(
                dx * dx + dy * dy);

            // Keep the selector inside the wheel.
            if (distance > radius)
            {
                dx = dx / distance * radius;
                dy = dy / distance * radius;

                distance = radius;
            }

            double angle = Math.Atan2(dy, dx)
                * 180.0 / Math.PI;

            if (angle < 0)
                angle += 360;

            double saturation = distance / radius;

            SetWheelPosition(angle, saturation);

            Color color = GetColorFromHsv(
                angle,
                saturation,
                1);

            UpdateHexInput(color);

            ColorChanged?.Invoke(color);
        }

        private void SetWheelPosition(
            double hue,
            double saturation)
        {
            double center = WheelSize / 2.0;
            double radius = center - 1;

            double angle = hue * Math.PI / 180.0;

            double x = center +
                Math.Cos(angle) * saturation * radius;

            double y = center +
                Math.Sin(angle) * saturation * radius;

            Canvas.SetLeft(PickerIndicator, x - 8);
            Canvas.SetTop(PickerIndicator, y - 8);
        }

        private void UpdateHexInput(Color color)
        {
            _isUpdating = true;

            HexInput.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            _isUpdating = false;
        }

        private void HexInput_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isUpdating || !IsLoaded)
                return;

            try
            {
                if (ColorConverter.ConvertFromString(HexInput.Text)
                    is not Color color)
                    return;

                var hsv = GetHsvFromColor(color);

                SetWheelPosition(hsv.H, hsv.S);

                ColorChanged?.Invoke(color);
            }
            catch (FormatException)
            {
                // Ignore incomplete or invalid HEX input.
            }
        }

        private static Color GetColorFromHsv(
            double h,
            double s,
            double v)
        {
            double c = v * s;

            double x = c * (
                1 - Math.Abs((h / 60) % 2 - 1));

            double m = v - c;

            double r, g, b;

            if (h < 60)
            {
                r = c;
                g = x;
                b = 0;
            }
            else if (h < 120)
            {
                r = x;
                g = c;
                b = 0;
            }
            else if (h < 180)
            {
                r = 0;
                g = c;
                b = x;
            }
            else if (h < 240)
            {
                r = 0;
                g = x;
                b = c;
            }
            else if (h < 300)
            {
                r = x;
                g = 0;
                b = c;
            }
            else
            {
                r = c;
                g = 0;
                b = x;
            }

            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }

        private static (double H, double S, double V)
            GetHsvFromColor(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double delta = max - min;

            double h = 0;

            if (delta != 0)
            {
                if (max == r)
                    h = 60 * (((g - b) / delta) % 6);
                else if (max == g)
                    h = 60 * ((b - r) / delta + 2);
                else
                    h = 60 * ((r - g) / delta + 4);
            }

            if (h < 0)
                h += 360;

            double s = max == 0 ? 0 : delta / max;

            return (h, s, max);
        }
    }
}
