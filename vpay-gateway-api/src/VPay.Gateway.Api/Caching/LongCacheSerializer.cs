using Polly.Caching;

namespace VPay.Gateway.Api.Caching;

internal sealed class LongCacheSerializer : ICacheItemSerializer<long, string>
{
    public long Deserialize(string objectToDeserialize) =>
        long.Parse(objectToDeserialize);

    public string Serialize(long objectToSerialize) =>
        objectToSerialize.ToString();
}
