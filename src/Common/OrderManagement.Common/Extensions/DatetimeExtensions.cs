namespace OrderManagement.Common.Extensions;

public static class DateTimeExtensions
{
    public static string ToIso8601String(this DateTime dateTime)
        => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    public static bool IsInPast(this DateTime dateTime)
        => dateTime < DateTime.UtcNow;

    public static bool IsInFuture(this DateTime dateTime)
        => dateTime > DateTime.UtcNow;
}