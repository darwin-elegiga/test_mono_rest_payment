using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    /// <summary>
    /// This is used to create a <see cref="DocDistMsg" />
    /// </summary>
    public interface IDistributionMessageBuilder
    {
        /// <summary>
        /// Creates a <see cref="DocDistMsg"/> using the <see cref="StatusRecord"/>s  <param name="statusFile"></param>
        /// </summary>
        /// <param name="statusFile"></param>
        /// <param name="statusFilePath"></param>
        /// <param name="statusFileId"></param>
        /// <param name="isCustomSendBackTpa"></param>
        /// <param name="warningDelayInHours"></param>
        /// <returns></returns>
        DocDistMsg CreateDistributionMessage(StatusFile statusFile, string statusFilePath, long statusFileId, bool isCustomSendBackTpa, int warningDelayInHours);
    }
}
