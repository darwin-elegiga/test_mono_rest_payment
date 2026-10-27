using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IAuxiliaryFileWriter
    {
        Task<string> Write(string statusFilePath, long fileId, StatusFile statusFile, bool isReissue, bool checkAuxFileGenerated = true, bool updateStatus = true);
    }
}
