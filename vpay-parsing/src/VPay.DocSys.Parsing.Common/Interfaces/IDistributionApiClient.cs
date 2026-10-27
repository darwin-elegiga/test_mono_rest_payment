using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IDistributionApiClient
    {
        /// <summary>
        /// Will call the endpoint to transfer the given <paramref name="filePaths"/>
        /// using the <paramref name="scpSettings"/> and generate the associated control files.
        /// </summary>
        /// <param name="scpSettings">setting used to connect to the server from the endpoint</param>
        /// <param name="filePaths">list of files to transfer</param>
        /// <returns></returns>
        Task<IList<ServerFile>> FtpFiles(ScpSettings scpSettings, params string[] filePaths);

        /// <summary>
        /// Will call the endpoint to transfer the given <paramref name="filePaths" />
        /// using the <paramref name="scpSettings" /> and, optionally, create the associated control files.
        /// </summary>
        /// <param name="scpSettings">setting used to connect to the server from the endpoint</param>
        /// <param name="createCtrlFiles">if set to <c>true</c> the associated control files will be also be created.</param>
        /// <param name="filePaths">list of files to transfer</param>
        /// <returns></returns>
        Task<IList<ServerFile>> FtpFiles(ScpSettings scpSettings, bool createCtrlFiles, params string[] filePaths);
    }
}
