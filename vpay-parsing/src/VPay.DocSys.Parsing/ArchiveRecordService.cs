using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Data.Abstractions.Repositories;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing
{
    public class ArchiveRecordService : IArchiveRecordService
    {
        private readonly IArchiveRepository _archiveRepository;
        private readonly ILogger _logger;
        public ArchiveRecordService(IArchiveRepository archiveRepository, ILoggerFactory loggerFactory)
        {
            _archiveRepository = archiveRepository ?? throw new ArgumentNullException(nameof(archiveRepository));
            _logger = loggerFactory.CreateLogger<ArchiveRecordService>();
        }
        public async Task RestoreByStatusFileId(int statusFileId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Calling [archive].[usp_Restore_ByPaymentStatusFileId] Stored Procedure with StatusFileId: {statusFileId}", statusFileId);
            try
            {
                await _archiveRepository.RestoreByStatusFile(statusFileId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task ArchiveByStatusFileId(int statusFileId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Calling [archive].[usp_TruncateAndArchive_ByPaymentStatusFileId] Stored Procedure with StatusFileId: {statusFileId}", statusFileId);
            try
            {
                await _archiveRepository.TruncateAndArchiveByStatusFile(statusFileId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
