
namespace LumaCtrl.Services.Protocols
{
    public sealed class ElkBledobProtocol : IDeviceProtocol
    {
        public byte[] BuildColorCommand(byte r, byte g, byte b)
        {
            return
            [
                0x7E, 0x07, 0x05, 0x03,
                g, r, b,
                0x10, 0xEF
            ];
        }
    }
}