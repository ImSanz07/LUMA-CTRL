
using LumaCtrl.Models;
using LumaCtrl.Services.Protocols;

namespace LumaCtrl.Services
{
    public class DeviceProfileService
    {
        private static readonly IReadOnlyList<DeviceProfile> Profiles =
        [
            new DeviceProfile
            {
                Name = "ELK-BLEDOB",
                DeviceAddress = "BE27E00003FA",
                Protocol = new ElkBledobProtocol()
            }
        ];

        public DeviceProfile GetProfile(string deviceAddress)
        {
            return Profiles.FirstOrDefault(
                profile => string.Equals(
                    profile.DeviceAddress,
                    deviceAddress,
                    StringComparison.OrdinalIgnoreCase))
                ?? new DeviceProfile
                {
                    Name = "Generic BLE Controller",
                    DeviceAddress = deviceAddress,
                    Protocol = new GenericBleProtocol()
                };
        }
    }
}
