namespace VPay.Gateway.Common.PlatformConfig;

public class ExternalClientConfigurationDto
{
    public int Id { get; set; }

    public string ClientCode { get; set; } = default!;

    public string ImplementationType { get; set; } = default!;

    public string Url { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string? Password { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
