
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LumaCtrl.Services;

namespace LumaCtrl
{
    public partial class MainWindow : Window
    {
        #region Fields

        private Color _selectedColor = Color.FromRgb(255, 69, 58);

        private readonly BluetoothService _bluetoothService = new();
        private readonly CustomPresetService _customPresetService = new();

        private string?[] _customPresets = new string?[5];

        private CancellationTokenSource? _brightnessDebounce;
        private CancellationTokenSource? _colorDebounce;
        private CancellationTokenSource? _effectCancellation;

        private volatile ScreenStats? _latestStats;

        #endregion

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();

            _customPresets = _customPresetService.LoadPresets();

            RefreshCustomPresetButtons();
            UpdateColorPreview();
        }

        #endregion

        #region Aurora Effect

        private async void StartAurora_Click(object sender, RoutedEventArgs e)
        {
            if (!_bluetoothService.IsConnected)
            {
                MessageBox.Show("Connect to the LED controller first.");
                return;
            }

            StopCurrentEffect();

            _effectCancellation = new CancellationTokenSource();
            CancellationToken token = _effectCancellation.Token;

            try
            {
                await RunAuroraAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Effect was stopped or replaced.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Aurora Effect Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task RunAuroraAsync(CancellationToken token)
        {
            Color[] colours =
            {
                Color.FromRgb(0, 220, 255),
                Color.FromRgb(100, 40, 255),
                Color.FromRgb(255, 50, 170),
                Color.FromRgb(100, 40, 255)
            };

            int current = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                Color from = colours[current];
                Color to = colours[(current + 1) % colours.Length];

                const int steps = 80;

                for (int i = 0; i <= steps; i++)
                {
                    token.ThrowIfCancellationRequested();

                    double progress = (double)i / steps;

                    byte r = (byte)(from.R + (to.R - from.R) * progress);
                    byte g = (byte)(from.G + (to.G - from.G) * progress);
                    byte b = (byte)(from.B + (to.B - from.B) * progress);

                    double brightness = BrightnessSlider.Value / 100.0;

                    r = (byte)(r * brightness);
                    g = (byte)(g * brightness);
                    b = (byte)(b * brightness);

                    Color output = Color.FromRgb(r, g, b);

                    ColorPreview.Background = new SolidColorBrush(output);

                    RedValue.Text = r.ToString();
                    GreenValue.Text = g.ToString();
                    BlueValue.Text = b.ToString();

                    await _bluetoothService.SendColorAsync(r, g, b);

                    await Task.Delay(50, token);
                }

                current = (current + 1) % colours.Length;
            }
        }

        #endregion

        #region Ember Effect

        private async void StartEmber_Click(object sender, RoutedEventArgs e)
        {
            if (!_bluetoothService.IsConnected)
            {
                MessageBox.Show("Connect to the LED controller first.");
                return;
            }

            StopCurrentEffect();

            _effectCancellation = new CancellationTokenSource();

            try
            {
                await RunEmberAsync(_effectCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Effect stopped.
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ember Effect Failed");
            }
        }

        private async Task RunEmberAsync(CancellationToken token)
        {
            var random = new Random();

            Color baseColor = Color.FromRgb(145, 24, 5);
            Color glowColor = Color.FromRgb(210, 55, 8);
            Color flareColor = Color.FromRgb(255, 105, 20);

            double brightness = 0.65;
            double targetBrightness = 0.65;

            double flareIntensity = 0;
            double targetFlare = 0;

            int flareCooldown = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (random.NextDouble() < 0.012)
                {
                    targetBrightness = 0.48 + random.NextDouble() * 0.20;
                }

                brightness += (targetBrightness - brightness) * 0.035;

                if (flareCooldown <= 0 && random.NextDouble() < 0.018)
                {
                    targetFlare = 0.35 + random.NextDouble() * 0.45;
                    flareCooldown = random.Next(35, 75);
                }

                flareCooldown--;

                flareIntensity += (targetFlare - flareIntensity) * 0.045;

                if (flareIntensity > 0.30)
                {
                    targetFlare = 0;
                }

                double warmth = flareIntensity;

                byte r = (byte)(
                    baseColor.R +
                    (glowColor.R - baseColor.R) * warmth);

                byte g = (byte)(
                    baseColor.G +
                    (glowColor.G - baseColor.G) * warmth);

                byte b = (byte)(
                    baseColor.B +
                    (glowColor.B - baseColor.B) * warmth);

                if (flareIntensity > 0.35)
                {
                    double amber = (flareIntensity - 0.35) * 0.35;

                    r = (byte)Math.Min(255, r + (flareColor.R - r) * amber);
                    g = (byte)Math.Min(255, g + (flareColor.G - g) * amber);
                    b = (byte)Math.Min(255, b + (flareColor.B - b) * amber);
                }

                double finalBrightness =
                    brightness * (BrightnessSlider.Value / 100.0);

                r = (byte)(r * finalBrightness);
                g = (byte)(g * finalBrightness);
                b = (byte)(b * finalBrightness);

                Color output = Color.FromRgb(r, g, b);

                ColorPreview.Background = new SolidColorBrush(output);

                RedValue.Text = r.ToString();
                GreenValue.Text = g.ToString();
                BlueValue.Text = b.ToString();

                await _bluetoothService.SendColorAsync(r, g, b);

                await Task.Delay(100, token);
            }
        }

        #endregion

        #region Effect Controls

        private void StopCurrentEffect()
        {
            _effectCancellation?.Cancel();
            _effectCancellation?.Dispose();
            _effectCancellation = null;
        }

        private void StopEffect_Click(object sender, RoutedEventArgs e)
        {
            StopCurrentEffect();
        }

        #endregion

        #region Colour Controls

        private async Task SendCurrentColorAsync()
        {
            if (!_bluetoothService.IsConnected)
                return;

            double brightness = BrightnessSlider.Value / 100.0;

            byte r = (byte)(_selectedColor.R * brightness);
            byte g = (byte)(_selectedColor.G * brightness);
            byte b = (byte)(_selectedColor.B * brightness);

            try
            {
                await _bluetoothService.SendColorAsync(r, g, b);
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Text = "COMMAND ERROR";

                MessageBox.Show(
                    ex.Message,
                    "LED Command Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ColorPicker_ColorChanged(Color color)
        {
            _selectedColor = color;

            UpdateColorPreview();

            _colorDebounce?.Cancel();
            _colorDebounce?.Dispose();

            var cancellation = new CancellationTokenSource();
            _colorDebounce = cancellation;

            try
            {
                await Task.Delay(80, cancellation.Token);
                await SendCurrentColorAsync();
            }
            catch (TaskCanceledException)
            {
                // A newer colour selection arrived.
            }
        }

        private async void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not string hex)
                return;

            _selectedColor = (Color)ColorConverter.ConvertFromString(hex);

            UpdateColorPreview();

            await SendCurrentColorAsync();
        }

        private async void BrightnessSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;

            UpdateColorPreview();

            _brightnessDebounce?.Cancel();
            _brightnessDebounce?.Dispose();

            var cancellation = new CancellationTokenSource();
            _brightnessDebounce = cancellation;

            try
            {
                await Task.Delay(120, cancellation.Token);
                await SendCurrentColorAsync();
            }
            catch (TaskCanceledException)
            {
                // Another slider update arrived.
            }
        }

        private void UpdateColorPreview()
        {
            double brightness = BrightnessSlider.Value / 100.0;

            byte r = (byte)(_selectedColor.R * brightness);
            byte g = (byte)(_selectedColor.G * brightness);
            byte b = (byte)(_selectedColor.B * brightness);

            Color adjustedColor = Color.FromRgb(r, g, b);

            ColorPreview.Background = new SolidColorBrush(adjustedColor);

            RedValue.Text = r.ToString();
            GreenValue.Text = g.ToString();
            BlueValue.Text = b.ToString();

            BrightnessValue.Text = $"{BrightnessSlider.Value:0}%";
        }

        #endregion

        #region Bluetooth Connection

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = false;
                ConnectButton.Content = "CONNECTING...";

                ConnectionStatusText.Text = "CONNECTING...";
                ConnectionStatusText.Foreground =
                    new SolidColorBrush(Color.FromRgb(255, 184, 77));

                await _bluetoothService.ConnectAsync();

                ConnectionStatusText.Text = "CONNECTED";
                ConnectionStatusText.Foreground =
                    new SolidColorBrush(Color.FromRgb(113, 219, 145));

                ConnectButton.Content = "CONNECTED";
            }
            catch (Exception ex)
            {
                _bluetoothService.Disconnect();

                ConnectionStatusText.Text = "CONNECTION FAILED";
                ConnectionStatusText.Foreground =
                    new SolidColorBrush(Color.FromRgb(255, 107, 107));

                ConnectButton.Content = "RETRY CONNECTION";

                MessageBox.Show(
                    ex.Message,
                    "LUMA//CTRL - Bluetooth Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        #endregion

        #region Custom Presets

        private void CustomPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            int index = Convert.ToInt32(button.Tag);

            if (string.IsNullOrEmpty(_customPresets[index]))
            {
                _customPresets[index] = GetSelectedColorHex();

                SaveCustomPresets();
                RefreshCustomPresetButtons();

                return;
            }

            _selectedColor = (Color)ColorConverter.ConvertFromString(
                _customPresets[index])!;

            UpdateColorPreview();

            _ = SendCurrentColorAsync();
        }

        private string GetSelectedColorHex()
        {
            return $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
        }

        private void SaveCustomPresets()
        {
            try
            {
                _customPresetService.SavePresets(_customPresets);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Save Presets",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshCustomPresetButtons()
        {
            Button[] buttons =
            {
                CustomPreset1Button,
                CustomPreset2Button,
                CustomPreset3Button,
                CustomPreset4Button,
                CustomPreset5Button
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                string? hex = _customPresets[i];

                if (string.IsNullOrEmpty(hex))
                {
                    button.Content = "+";

                    button.Background = new SolidColorBrush(
                        Color.FromRgb(27, 32, 38));

                    button.Foreground = new SolidColorBrush(
                        Color.FromRgb(120, 131, 141));
                }
                else
                {
                    Color color = (Color)ColorConverter.ConvertFromString(hex)!;

                    button.Content = $"{i + 6:00}";
                    button.Background = new SolidColorBrush(color);

                    button.Foreground = new SolidColorBrush(
                        Color.FromRgb(11, 13, 16));
                }

                button.ContextMenu = CreatePresetContextMenu();
            }
        }

        private ContextMenu CreatePresetContextMenu()
        {
            var menu = new ContextMenu();

            var overwriteItem = new MenuItem
            {
                Header = "Overwrite with current colour"
            };

            overwriteItem.Click += OverwriteCustomPreset_Click;

            var clearItem = new MenuItem
            {
                Header = "Clear preset"
            };

            clearItem.Click += ClearCustomPreset_Click;

            menu.Items.Add(overwriteItem);
            menu.Items.Add(clearItem);

            return menu;
        }

        private int GetContextMenuPresetIndex(object sender)
        {
            var menuItem = (MenuItem)sender;
            var menu = (ContextMenu)menuItem.Parent;
            var button = (Button)menu.PlacementTarget;

            return Convert.ToInt32(button.Tag);
        }

        private void OverwriteCustomPreset_Click(object sender, RoutedEventArgs e)
        {
            int index = GetContextMenuPresetIndex(sender);

            _customPresets[index] = GetSelectedColorHex();

            SaveCustomPresets();
            RefreshCustomPresetButtons();
        }

        private void ClearCustomPreset_Click(object sender, RoutedEventArgs e)
        {
            int index = GetContextMenuPresetIndex(sender);

            _customPresets[index] = null;

            SaveCustomPresets();
            RefreshCustomPresetButtons();
        }

        #endregion

        #region Screen Reactive Effect

        private async Task RunScreenReactiveAsync(CancellationToken token)
        {
            const double gain = 1.6;
            const double attackTau = 0.07;
            const double releaseTau = 0.30;
            const double colorTau = 0.25;
            const double onThreshold = 0.05;
            const double offThreshold = 0.025;
            const double minLevel = 0.05;
            const double hueFreezeBelow = 0.06;

            const int frameMs = 33;
            const int sampleMs = 33;
            const int deadband = 2;

            using var cts =
                CancellationTokenSource.CreateLinkedTokenSource(token);

            var samplerTask = Task.Run(async () =>
            {
                using var sampler = new ScreenSampler();

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        _latestStats = sampler.Sample();
                    }
                    catch
                    {
                        // Keep the last valid sample.
                    }

                    try
                    {
                        await Task.Delay(sampleMs, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });

            try
            {
                while (_latestStats == null)
                    await Task.Delay(10, token);

                double brightness = 0;

                double cr = 255;
                double cg = 255;
                double cb = 255;

                bool lit = false;

                int lastR = -1;
                int lastG = -1;
                int lastB = -1;

                var sw = System.Diagnostics.Stopwatch.StartNew();
                double last = 0;

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    double frameStart = sw.Elapsed.TotalSeconds;
                    double dt = Math.Min(frameStart - last, 0.2);

                    last = frameStart;

                    ScreenStats s = _latestStats!;

                    double raw = Math.Min(1.0, s.Brightness * gain);

                    if (!lit && raw > onThreshold)
                        lit = true;
                    else if (lit && raw < offThreshold)
                        lit = false;

                    double target = lit
                        ? Math.Max(raw, minLevel)
                        : 0.0;

                    if (s.Brightness * gain > hueFreezeBelow)
                    {
                        double k = Math.Clamp(s.Chroma / 0.12, 0.0, 1.0);

                        double tr = 255 + (s.R - 255) * k;
                        double tg = 255 + (s.G - 255) * k;
                        double tb = 255 + (s.B - 255) * k;

                        double ck = 1.0 - Math.Exp(-dt / colorTau);

                        cr += (tr - cr) * ck;
                        cg += (tg - cg) * ck;
                        cb += (tb - cb) * ck;
                    }

                    double tau = target > brightness
                        ? attackTau
                        : releaseTau;

                    brightness +=
                        (target - brightness) *
                        (1.0 - Math.Exp(-dt / tau));

                    double level =
                        (brightness < offThreshold * 0.5 && target == 0)
                            ? 0
                            : brightness;

                    double scale =
                        level * (BrightnessSlider.Value / 100.0);

                    int outR = Math.Clamp(
                        (int)Math.Round(cr * scale), 0, 255);

                    int outG = Math.Clamp(
                        (int)Math.Round(cg * scale), 0, 255);

                    int outB = Math.Clamp(
                        (int)Math.Round(cb * scale), 0, 255);

                    bool turnedOff =
                        outR == 0 && outG == 0 && outB == 0 &&
                        (lastR != 0 || lastG != 0 || lastB != 0);

                    int maxOut = Math.Max(outR, Math.Max(outG, outB));
                    int db = maxOut < 40 ? 1 : deadband;

                    bool changed =
                        Math.Abs(outR - lastR) >= db ||
                        Math.Abs(outG - lastG) >= db ||
                        Math.Abs(outB - lastB) >= db;

                    if (turnedOff || changed)
                    {
                        await _bluetoothService.SendColorAsync(
                            (byte)outR,
                            (byte)outG,
                            (byte)outB);

                        lastR = outR;
                        lastG = outG;
                        lastB = outB;
                    }

                    int spentMs = (int)(
                        (sw.Elapsed.TotalSeconds - frameStart) * 1000.0);

                    await Task.Delay(
                        Math.Max(1, frameMs - spentMs),
                        token);
                }
            }
            finally
            {
                cts.Cancel();

                try
                {
                    await samplerTask;
                }
                catch
                {
                    // Sampler stopped.
                }
            }
        }

        private async void StartScreenReactive_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_bluetoothService.IsConnected)
            {
                MessageBox.Show("Connect to the LED controller first.");
                return;
            }

            StopCurrentEffect();

            _effectCancellation = new CancellationTokenSource();

            try
            {
                await RunScreenReactiveAsync(_effectCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Effect stopped.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Screen Reactive Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Window Cleanup

        protected override void OnClosed(EventArgs e)
        {
            StopCurrentEffect();

            _brightnessDebounce?.Cancel();
            _colorDebounce?.Cancel();

            _bluetoothService.Disconnect();

            base.OnClosed(e);
        }

        #endregion
    }
}
