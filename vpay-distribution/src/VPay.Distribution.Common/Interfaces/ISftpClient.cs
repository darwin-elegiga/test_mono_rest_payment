using System;
using System.IO;

namespace VPay.Distribution.Common
{
    public interface ISftpClient : IDisposable
    {
        void Connect();
        void Disconnect();
        void UploadFile(Stream input, string path, bool canOverride);
    }
}
