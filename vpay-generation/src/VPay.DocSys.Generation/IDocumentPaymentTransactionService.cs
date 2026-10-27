using System.Threading.Tasks;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation
{
    public interface IDocumentPaymentTransactionService
    {
        Task<PaymentTransaction> GetDocumentPaymentDetails(long transactionId);
    }
}
