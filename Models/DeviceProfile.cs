
using LumaCtrl.Services.Protocols;

namespace LumaCtrl.Models
{
    public class DeviceProfile
    {
        public string Name { get; init; } = string.Empty;

        public string DeviceAddress { get; init; } = string.Empty;

        public IDeviceProtocol Protocol { get; init; } = null!;
    }
}