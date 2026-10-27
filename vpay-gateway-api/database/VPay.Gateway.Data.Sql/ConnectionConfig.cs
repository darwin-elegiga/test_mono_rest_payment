namespace VPay.Gateway.Data.Sql;

public class ConnectionConfig
{
    public string Hostname { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IntegratedSecurity { get; set; }

    public string BaseConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The time in seconds to wait for the command to execute. The default is 30 seconds.
    /// </summary>
    public int? DefaultCommandTimeout { get; set; }
}
