namespace OrderManagement.Common.Configuration;

public sealed class DatabaseConfig
{
    public const string SectionName = "Database";

    public required string ConnectionString { get; init; }

    public int CommandTimeout { get; init; } = 30;

    public bool EnableSensitiveDataLogging { get; init; } = false;

    public int MaxRetryCount { get; init; } = 3;
}