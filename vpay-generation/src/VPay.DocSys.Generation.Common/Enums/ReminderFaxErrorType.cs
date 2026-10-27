using System.ComponentModel;

namespace VPay.DocSys.Generation.Common
{
    public enum ReminderFaxErrorType
    {
        /// <summary>
        /// Missing OTB Information, such asfFull names for TPA or BE, and Origin and Voice Phone Numbers for Medical TPAs
        /// </summary>
        [Description("Missing OTB Information")]
        MissingOtbInformation,

        /// <summary>
        /// Missing Distribution Packet, when no distribution pdf could be found for the given transaction
        /// </summary>
        [Description("Missing Distribution Packet")]
        MissingDistributionPacket,

        /// <summary>
        /// Cannot Resolve Payor Name, when no payor name could be resolved for the given Reminder Fax
        /// </summary>
        [Description("Cannot Resolve Payor Name")]
        CannotResolvePayorName,

        /// <summary>
        /// Unhandled <see cref="Exception"/> caught, but not thrown, logged, instead
        /// </summary>
        [Description("Unhandled Exception Caught")]
        GenericError,
    }
}
