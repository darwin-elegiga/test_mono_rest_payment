using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution
{
    public class STPDistributionMessageQueueProcessor : ISTPMessageQueueProcessor
    {
        private readonly ILogger _logger;
        private readonly IStpPaymentService _stpPaymentService;

        public STPDistributionMessageQueueProcessor(
            IStpPaymentService stpPaymentService,
            ILogger<STPDistributionMessageQueueProcessor> logger)
        {
            _stpPaymentService = stpPaymentService ?? throw new ArgumentNullException(nameof(stpPaymentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DistributionProcessResult> ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"Processor", this.GetType().Name },
                {"STSFile", message.StatusFilePath},
                {"TPA", message.TPA },
                {"ClientCode", message.TPA },
            }))
            {
                try
                {
                    foreach (var transaction in message.Transactions)
                    {
                        _logger.LogInformation("RECEIVED Payment in STP Queue with Transaction ID {TransactionId}", transaction.TxnId);

                        StpPayment stpPayment = await _stpPaymentService
                            .GetStpPayment(transaction.TxnId, cancellationToken)
                            .ConfigureAwait(false);

                        if (stpPayment != null)
                        {
                            if (stpPayment.IsB2B())
                            {
                                _logger.LogInformation("STP Queue Marking Remit Valid for STP payment with Transaction ID {TransactionId} ({EnrollmentType}, {FileType})", transaction.TxnId, stpPayment.EnrollType, stpPayment.Edi835Type);

                                await _stpPaymentService
                                    .MarkRemitValid(transaction.TxnId, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            else if (stpPayment.IsC2B())
                            {
                                _logger.LogInformation("STP Queue Sending PMB message to iSeries for STP Payment with Transaction ID {TransactionId} ({EnrollmentType}, {FileType})", transaction.TxnId, stpPayment.EnrollType, stpPayment.Edi835Type);

                                await _stpPaymentService
                                    .PublishPmbMessage(transaction.TxnId, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("STP Queue Sending PMB message to iSeries for payment with Transaction ID {TransactionId}", transaction.TxnId);

                            await _stpPaymentService
                                .PublishPmbMessage(transaction.TxnId, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }

                    return new DistributionProcessResult("STP");
                }
                catch (Exception ex)
                {
                    _logger.LogError("ERROR STP Queue Sending PMB message to iSeries with exception {ex}", ex.Message);
                    throw;
                }
            }
        }
    }
}
