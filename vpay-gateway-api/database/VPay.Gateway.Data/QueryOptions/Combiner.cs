namespace VPay.Gateway.Data.QueryOptions;

/// <summary>
/// Splits results into two objects, then combines them.
/// </summary>
/// <typeparam name="TFirst">The type of the object in the first portion of results.</typeparam>
/// <typeparam name="TSecond">The type of the object in the second portion of results.</typeparam>
/// <typeparam name="TResult">The resulting object type.</typeparam>
public sealed class Combiner<TFirst, TSecond, TResult>
{
    public string SplitOn { get; }

    public Func<TFirst, TSecond, TResult> Combine { get; }

    public Combiner(string splitOn, Func<TFirst, TSecond, TResult> combine)
    {
        SplitOn = splitOn;
        Combine = combine;
    }
}
