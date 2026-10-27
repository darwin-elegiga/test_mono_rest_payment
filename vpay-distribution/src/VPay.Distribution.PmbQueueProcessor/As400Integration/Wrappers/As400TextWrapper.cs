using com.ibm.as400.access;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Wrappers;

public class As400TextWrapper : IAs400Text
{
    private readonly AS400Text _as400Text;

    public As400TextWrapper(AS400Text as400Text)
    {
        _as400Text = as400Text ?? throw new ArgumentNullException(nameof(as400Text));
    }

    public byte[] ToBytes(string data)
    {
        return _as400Text.toBytes(data);
    }
}
