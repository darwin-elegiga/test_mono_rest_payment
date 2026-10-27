using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public interface ISshOperations
    {
        /// <summary>
        /// Will transfer the given file (with path) via SCP to the host given in <paramref name="scpSettings" />
        /// </summary>
        /// <param name="filePath">full file path of file to transfer</param>
        /// <param name="scpSettings">settings used to connect</param>
        Task TransferFileAsync(string filePath, ScpSettings scpSettings);

        /// <summary>
        /// Will touch the given file name via SSH to the host given in <paramref name="scpSettings" />
        /// </summary>
        /// <param name="fileName">file name to create</param>
        /// <param name="scpSettings">settings used to connect</param>
        Task TouchControlFileOnServerAsync(string fileName, ScpSettings scpSettings);
    }
}
