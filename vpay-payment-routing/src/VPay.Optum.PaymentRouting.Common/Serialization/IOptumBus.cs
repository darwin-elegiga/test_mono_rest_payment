using MassTransit;

namespace VPay.Optum.PaymentRouting.Common.Serialization
{
    /// <summary>
    /// MassTransit Bus for handling Optum's messages.
    /// </summary>
    public interface IOptumBus : IBus
    {
    }

    /// <summary>
    /// MassTransit Bus for handling Optum's AP/REAP messages on 'ap-prs' queue.
    /// This is an additional Azure bus for MassTransit MultiBus configuration.
    /// This bus uses AVRO binary serialization but no AVRO encoding.
    /// First message byte is the first payload byte.
    /// </summary>
    public interface IOptumApBus : IOptumBus
    {
    }

    /// <summary>
    /// MassTransit Bus for handling Optum's Payment Record messages on 'vpay-prs' queue.
    /// This is an additional Azure bus for MassTransit MultiBus configuration.
    /// This bus uses AVRO binary serialization and AVRO encoding.
    /// The encoding adds two magic bytes and eight bytes for the schema fingerprint to the beginning of each message.
    /// First ten messages bytes are used for the encoding. Eleventh message byte is the first payload byte.
    /// </summary>
    public interface IOptumPrBus : IOptumBus
    {
    }

    /// <summary>
    /// MassTransit Bus for handling Optum's Payment Summary messages on 'vpay-funding-prs' queue.
    /// This is an additional Azure bus for MassTransit MultiBus configuration.
    /// This bus uses AVRO binary serialization and AVRO encoding.
    /// The encoding adds two magic bytes and eight bytes for the schema fingerprint to the beginning of each message.
    /// First ten messages bytes are used for the encoding. Eleventh message byte is the first payload byte.
    /// </summary>
    public interface IOptumPsBus : IOptumBus
    {
    }

    /// <summary>
    /// MassTransit Bus for handling VPay Rabbit messages to the OptumWrap virtualHost.
    /// Specifically used to add Optum/CreateCtrStatus messages to indicate that the
    /// file transfer was successful or not for CLCFT payments.
    /// </summary>
    public interface IOptumWrapBus : IBus
    {
    }

}
