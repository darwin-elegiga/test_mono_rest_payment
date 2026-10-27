using com.optum.avro.schema;
using VPay.MassTransit.Serialization;

namespace VPay.Optum.PaymentRouting.Common.Serialization
{
    /// <summary>
    /// Represents message with a funding payload serialized in an AVRO binary format.
    /// </summary>
    public class VpayPayment : Payer835Segment, IAvroMessage
    {

    }
}
