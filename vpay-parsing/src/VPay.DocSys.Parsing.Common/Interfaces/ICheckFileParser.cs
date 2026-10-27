using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface ICheckFileParser
    {
        /// <summary>
        /// Will parse the given <paramref name="checkFilePath"/> unless <paramref name="isRedCardTpa"/> is true
        /// If the file does not exist and <paramref name="isCustomSendBackTpa"/> is true, will retrieve
        /// </summary>
        /// <param name="checkFilePath">complete file path</param>
        /// <param name="isCustomSendBackTpa">flag for Custom SendBack TPA</param>
        /// <param name="isRedCardTpa">flag for RedCard TPA</param>
        /// <returns><see cref="IList{T}"/> of <see cref="CheckInfo"/></returns>
        IList<CheckInfo> ParseCheckFileAsync(string checkFilePath, bool isCustomSendBackTpa, bool isRedCardTpa);
    }
}
