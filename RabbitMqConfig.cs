namespace OrderManagement.Common.Configuration;

public sealed class RabbitMqConfig
{
    public const string SectionName = "RabbitMQ";

    public required string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public required string Username { get; init; } = "admin";

    public required string Password { get; init; } = "password123";

    public string VirtualHost { get; init; } = "/";

    public int RequestedHeartbeat { get; init; } = 60;
}
