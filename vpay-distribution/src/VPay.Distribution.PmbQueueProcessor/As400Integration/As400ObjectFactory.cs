using com.ibm.as400.access;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Wrappers;

namespace VPay.Distribution.PmbQueueProcessor.As400Integration;

public class As400ObjectFactory : IAs400ObjectFactory
{
    public IAs400 CreateAs400(string systemName, string userId, string password)
    {
        var as400 = new AS400();
        as400.setSystemName(systemName);
        as400.setUserId(userId);
        char[] passwordChars = password.ToCharArray();
        as400.setPassword(passwordChars);
        return new As400Wrapper(as400);
    }

    public IAs400Text CreateAs400Text(int length, IAs400 connection)
    {
        var as400Wrapper = (As400Wrapper)connection;
        var as400Text = new AS400Text(length, as400Wrapper.UnderlyingObject.getCcsid());
        return new As400TextWrapper(as400Text);
    }

    public IDataQueue CreateDataQueue(IAs400 connection, string path)
    {
        var as400Wrapper = (As400Wrapper)connection;
        var dq = new DataQueue(as400Wrapper.UnderlyingObject, path);
        return new DataQueueWrapper(dq);
    }
}
