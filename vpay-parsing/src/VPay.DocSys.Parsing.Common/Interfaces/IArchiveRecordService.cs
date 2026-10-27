using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IArchiveRecordService
    {
        /// <summary>
        /// Calls <see cref="VPay.DocSys.Data.IArchiveRepository.RestoreByStatusFileId"/>
        /// </summary>
        /// <param name="statusFileId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RestoreByStatusFileId(int statusFileId, CancellationToken cancellationToken);
        /// <summary>
        /// Calls <see cref="VPay.DocSys.Data.IArchiveRepository.TruncateAndArchiveByStatusFile"/>
        /// </summary>
        /// <param name="statusFileId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ArchiveByStatusFileId(int statusFileId, CancellationToken cancellationToken);
    }
}
