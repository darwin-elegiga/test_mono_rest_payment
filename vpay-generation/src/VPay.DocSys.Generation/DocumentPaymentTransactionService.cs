using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Data;

namespace VPay.DocSys.Generation
{
    public class DocumentPaymentTransactionService : IDocumentPaymentTransactionService
    {
        private readonly IDb2TransactionRepository _repository;
        private readonly ILogger<DocumentPaymentTransactionService> _logger;

        public DocumentPaymentTransactionService(
            IDb2TransactionRepository repository,
             ILogger<DocumentPaymentTransactionService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger;
        }

        public async Task<PaymentTransaction> GetDocumentPaymentDetails(long transactionId)
        {
            if (transactionId <= 0)
            {
                throw new ArgumentException("Transaction ID must be greater than zero.", nameof(transactionId));
            }
            var result = await _repository.GetPayerInfoByTransactionIdAsync(transactionId);
            return result;
        }
    }
}
