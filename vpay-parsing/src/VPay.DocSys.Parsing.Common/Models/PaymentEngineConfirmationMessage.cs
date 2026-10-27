namespace VPay.DocSys.Parsing.Common;

/// <summary>
/// Contains information about a Payment Engine Confirmation file to be processed.
/// </summary>
public class PaymentEngineConfirmationMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentEngineConfirmationMessage"/> class.
    /// </summary>
    public PaymentEngineConfirmationMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentEngineConfirmationMessage"/> class.
    /// </summary>
    /// <param name="filePath">The file path to use.</param>
    public PaymentEngineConfirmationMessage(string filePath) => FilePath = filePath;

    /// <summary>
    /// Gets or sets the full path and name of the Payment Engine Confirmation file.
    /// </summary>
    public string FilePath { get; set; }
}
