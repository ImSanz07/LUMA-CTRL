
namespace LumaCtrl.Services.Protocols
{
    public interface IDeviceProtocol
    {
        byte[] BuildColorCommand(byte r, byte g, byte b);
    }
}
