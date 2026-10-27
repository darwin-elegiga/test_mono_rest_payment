namespace VPay.Gateway.Application;

public static class DocumentPraStatuses
{
    /// <summary>
    /// This is a message we get from the Document system saying the Transaction id we sent wasn't found.
    /// </summary>
    public static readonly string TransactionMissing = "TRANSACTION_NOT_FOUND";

    /// <summary>
    /// If the transactionDate is before 2 days ago it will return a different status
    /// </summary>
    /// <param name="transactionDate"></param>
    public static string GetNotReadyString(DateTime transactionDate)
    {
        if (DateTime.Now.AddDays(-2) < transactionDate)
        {
            return "PRA_REQUESTED";
        }
        else
        {
            return "PRA_NOT_FOUND";
        }
    }
}
