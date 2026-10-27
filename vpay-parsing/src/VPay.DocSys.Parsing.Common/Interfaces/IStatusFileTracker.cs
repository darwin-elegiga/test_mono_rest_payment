using System.Collections.Generic;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IStatusFileTracker
    {
        int InsertStatusFile(string fileName, string filePath, string tpa, string originCode, string billingEntity);
        FileStatus GetFileStatus(int fileId);
        List<dynamic> GetDocumentStatus(string fileName);
        long GetFileId(string filePath);
        string GetStatusFileNameFromTransaction(long transactionId);
        void UpdateFileStatus(int fileId, int statusId);
        void CleanTransactionStatus(IList<long> transactionIds);
        void InsertFileTransactions(int fileId, List<DocumentTransactionMessage> messages);
        IDictionary<string, object> GetStatusFileSummary(string stsFilePath);
        bool CanConnect();
        StpPayment? GetStpPayment(long transactionId);
        IEnumerable<StpPayment> GetStpPayments(IEnumerable<long> transactionIds);
        void BulkInsertStpPayments(IEnumerable<long> transactionIds, string networkName, string enrollType, string edi835Type, bool remitValid = false, bool edi835Avail = false, bool paymentSent = false, bool pmbMessageSent = false);
    }
}
