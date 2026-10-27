using Ardalis.SmartEnum;

namespace VPay.Gateway.Api.Models;

/// <summary>
/// Represents the modality of a vendor payment decision, indicating whether a payment has had a decision for the payment type since all its claims were accepted.
/// Valid values are
/// PREISCHK (Check)
/// PREISDCP (Digital Card)
/// PREISEFT (ACH/EFT)
/// PREISREJ (Rejection, Pre-Transaction Void)
/// </summary>
/// <remarks>Use the predefined static instances to specify the type of vendor payment decision in business logic.
/// This class is intended to provide type safety and clarity when working with vendor payment decision states:
/// decision = VendorPaymentDecisionModality.FromName("PREISCHK");
/// decision = VendorPaymentDecisionModality.FromValue(1);
/// </remarks>
public class VendorPaymentDecisionModality : SmartEnum<VendorPaymentDecisionModality>
{
    public static readonly VendorPaymentDecisionModality PreTransactionIsCheck = new("PREISCHK", 1);
    public static readonly VendorPaymentDecisionModality PreTransactionIsDigitalCard = new("PREISDCP", 2);
    public static readonly VendorPaymentDecisionModality PreTransactionIsAchOrEft = new("PREISEFT", 3);
    public static readonly VendorPaymentDecisionModality PreTransactionIsRejectedClaim = new("PREISREJ", 4);

    public VendorPaymentDecisionModality(string name, int value) : base(name, value)
    {
    }

    /// <summary>
    /// List of rejected claim modalities.
    /// </summary>
    public static IEnumerable<VendorPaymentDecisionModality> Rejected => [PreTransactionIsRejectedClaim];

    /// <summary>
    /// List of accepted claim modalities.
    /// </summary>
    public static IEnumerable<VendorPaymentDecisionModality> Accepted => [PreTransactionIsCheck, PreTransactionIsDigitalCard, PreTransactionIsAchOrEft];

    /// <summary>
    /// List of all claim modalities.
    /// </summary>
    public static IEnumerable<VendorPaymentDecisionModality> All => [PreTransactionIsCheck, PreTransactionIsDigitalCard, PreTransactionIsAchOrEft, PreTransactionIsRejectedClaim];

    /// <summary>
    /// Determines if the given decision modality is for an accepted claim.
    /// </summary>
    /// <param name="modality">The decision modality.</param>
    /// <returns>True if decision modality is for an accepted claim; False otherwise.</returns>
    public static bool IsAccepted(string modality)
    {
        return Accepted.Any(m => m.Name.Equals(modality, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if the given decision modality is for a rejected claim.
    /// </summary>
    /// <param name="modality">The decision modality.</param>
    /// <returns>True if decision modality is for a rejected claim; False otherwise.</returns>
    public static bool IsRejected(string modality)
    {
        return Rejected.Any(m => m.Name.Equals(modality, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if the given decision modality is valid.
    /// </summary>
    /// <param name="modality">The decision modality.</param>
    /// <returns>True if decision modality is valid; False otherwise.</returns>
    public static bool IsValid(string modality)
    {
        return All.Any(m => m.Name.Equals(modality, StringComparison.OrdinalIgnoreCase));
    }
}
