namespace VPay.Gateway.Common.Guidewire.Models;

public sealed class GuidewireReplaySettings
{
    /// <summary>
    /// The maximum number of messages to replay.
    /// </summary>
    public int MaxMessageCount { get; set; } = 1;

    /// <summary>
    /// The minimum message age in minutes in the current status to be considered for replay.
    /// </summary>
    public int MinMessageAgeInMinutes { get; set; } = 15;

    /// <summary>
    /// The maximum message age in days in the current status to be considered for replay.
    /// </summary>
    public int MaxMessageAgeInDays { get; set; } = 30;
}
