
namespace LumaCtrl.Services.Protocols
{
    public sealed class GenericBleProtocol : IDeviceProtocol
    {
        public byte[] BuildColorCommand(byte r, byte g, byte b)
        {
            return
            [
                0x7E, 0x07, 0x05, 0x03,
                r, g, b,
                0x10, 0xEF
            ];
        }
    }
}