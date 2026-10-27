namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor.RabbitMq;

public class RabbitMqConfig
{
    public string[] Hostnames { get; set; }

    public string VirtualHost { get; set; } = "/";

    public int Port { get; set; } = 5672;

    public string Username { get; set; }

    public string Password { get; set; }

    public TimeSpan Heartbeat { get; set; } = TimeSpan.FromSeconds(0.0);
}
