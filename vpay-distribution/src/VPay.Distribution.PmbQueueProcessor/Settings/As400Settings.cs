namespace VPay.Distribution.PmbQueueProcessor.Settings;

public class As400Settings
{
    public string SystemName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DataQueueName { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public int ConnectionPoolMaxConnections { get; set; }
    public int ConnectionPoolInactivityTimeout { get; set; }
}
