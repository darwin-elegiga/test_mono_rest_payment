using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data
{
    public class DocumentPaymentTransactionRepository : DataRepository, IDocumentPaymentTransactionRepository
    {
        private readonly ILogger _logger;
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTransactionRepository"/> class.
        /// </summary>
        /// <param name="connection">The connection to use.</param>
        public DocumentPaymentTransactionRepository(IDocSysConnection connection, ILogger<DocumentPaymentTransactionRepository> logger)
            : base(connection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<PaymentTransaction> GetPaymentTransactionsAsync(long transactionId, CancellationToken cancellationToken = default)
        {

            try
            {
                var paymentDetails = await this.Connection.SelectSingleOrDefaultAsync<PaymentTransaction>(
                     commandType: CommandType.StoredProcedure,
                     commandText: "[dbo].[usp_GetDocumentPaymentDetailsByTransactionId]",
                     commandParameters: new { transactionId },
                     cancellationToken: cancellationToken);

                if (paymentDetails==null)
                {
                    _logger.LogInformation("No PaymentRecord found for transactionId: {transactionId}", transactionId);
                    return null; // Return null if no records are found 
                }
                return paymentDetails;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error calling stored procedure usp_GetDocumentPaymentDetailsByTransactionId");
                throw;
            }     
        }
    }
  
}
