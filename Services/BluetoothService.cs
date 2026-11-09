
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LumaCtrl.Models;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace LumaCtrl.Services
{
    public class BluetoothService
    {
        private BluetoothLEDevice? _device;
        private GattDeviceService? _service;
        private GattCharacteristic? _colorCharacteristic;

        private readonly DeviceProfileService _profileService = new();
        private DeviceProfile? _activeProfile;

        private const string DeviceAddress = "BE27E00003FA";

        private static readonly Guid ServiceUuid =
            Guid.Parse("0000fff0-0000-1000-8000-00805f9b34fb");

        private static readonly Guid CharacteristicUuid =
            Guid.Parse("0000fff3-0000-1000-8000-00805f9b34fb");

        public bool IsConnected =>
            _device?.ConnectionStatus == BluetoothConnectionStatus.Connected
            && _colorCharacteristic != null;

        private static void Log(string message)
        {
            Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] [BluetoothService] {message}");
        }

        public async Task ConnectAsync()
        {
            Disconnect();

            Log("Starting connection...");

            ulong address = Convert.ToUInt64(DeviceAddress, 16);

            Log("Requesting BluetoothLEDevice...");

            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            Log(_device == null
                ? "BluetoothLEDevice returned NULL."
                : "BluetoothLEDevice acquired successfully.");

            if (_device == null)
                throw new Exception("Could not find the Bluetooth device.");

            _activeProfile = _profileService.GetProfile(DeviceAddress);

            Log($"Selected device profile: {_activeProfile.Name}");

            Log("Requesting GATT service FFF0...");

            var serviceResult = await _device.GetGattServicesForUuidAsync(
                ServiceUuid,
                BluetoothCacheMode.Uncached);

            Log($"GATT service result: {serviceResult.Status}");

            if (serviceResult.Status != GattCommunicationStatus.Success ||
                serviceResult.Services.Count == 0)
                throw new Exception("Could not access GATT service FFF0.");

            _service = serviceResult.Services[0];

            Log("Requesting characteristic FFF3...");

            var characteristicResult =
                await _service.GetCharacteristicsForUuidAsync(
                    CharacteristicUuid,
                    BluetoothCacheMode.Uncached);

            Log($"Characteristic result: {characteristicResult.Status}");

            if (characteristicResult.Status != GattCommunicationStatus.Success ||
                characteristicResult.Characteristics.Count == 0)
                throw new Exception("Could not access characteristic FFF3.");

            _colorCharacteristic = characteristicResult.Characteristics[0];

            Log("Connection setup completed successfully.");
        }

        public async Task SendColorAsync(byte r, byte g, byte b)
        {
            if (_activeProfile == null)
                throw new InvalidOperationException(
                    "Bluetooth device is not connected.");

            byte[] command =
                _activeProfile.Protocol.BuildColorCommand(r, g, b);

            await WriteCommandAsync(command, "LED command");
        }

        public async Task SendBrightnessAsync(byte percent)
        {
            percent = Math.Min(percent, (byte)100);

            byte[] command =
            {
                0x7E,
                0x04,
                0x01,
                percent,
                0x01,
                0xFF,
                0xFF,
                0x00,
                0xEF
            };

            await WriteCommandAsync(command, "Brightness command");
        }

        private async Task WriteCommandAsync(byte[] command, string operation)
        {
            if (_colorCharacteristic == null)
                throw new InvalidOperationException(
                    "Bluetooth device is not connected.");

            using var writer = new DataWriter();
            writer.WriteBytes(command);

            var result = await _colorCharacteristic.WriteValueAsync(
                writer.DetachBuffer(),
                GattWriteOption.WriteWithoutResponse);

            if (result != GattCommunicationStatus.Success)
                throw new Exception($"{operation} failed: {result}");
        }

        public void Disconnect()
        {
            _activeProfile = null;
            _colorCharacteristic = null;

            _service?.Dispose();
            _service = null;

            _device?.Dispose();
            _device = null;
        }
    }
}
