namespace PricingEngine.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "pricing_audit";
    public string Password { get; set; } = "pricing_audit_password";
    public string QueueName { get; set; } = "quote-audit";
}
