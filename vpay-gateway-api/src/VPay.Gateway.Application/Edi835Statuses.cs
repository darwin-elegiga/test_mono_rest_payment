namespace VPay.Gateway.Application;

public static class Edi835Statuses
{
    /// <summary>
    /// This is a message we get from the Document system saying the Transaction id we sent wasn't found.
    /// </summary>
    public static readonly string TransactionMissing = "TRANSACTION_NOT_FOUND";

    /// <summary>
    /// This is a message we get from the Document system saying 835 is not going to be generated because it was not requested.
    /// </summary>
    public static readonly string NotNeededFromDocSys = "835_NOT_NEEDED";

    /// <summary>
    /// This is what we return when an 835 is not needed
    /// </summary>
    public static readonly string NotNeeded = "835_NOT_REQUESTED";

    /// <summary>
    /// If the transactionDate is before 2 days ago it will return a different status
    /// </summary>
    /// <param name="transactionDate"></param>
    /// <returns></returns>
    public static string GetNotReadyString(DateTime transactionDate)
    {
        if (DateTime.Now.AddDays(-2) < transactionDate)
        {
            return "835_REQUESTED";
        }
        else
        {
            return "835_NOT_FOUND";
        }
    }
}
