using com.optum.apreap.avro;
using VPay.MassTransit.Serialization;

namespace VPay.Optum.PaymentRouting.Common.Serialization
{
    /// <summary>
    /// Represents message with an AP/REAP payload serialized in an AVRO binary format.
    /// </summary>
    public class VpayApReap : ApReapPaymentsAvro, IAvroMessage
    {

    }
}
