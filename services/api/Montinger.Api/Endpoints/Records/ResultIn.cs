namespace Montinger.Api.Endpoints.Records;

public record ResultIn(
    string? ResultId,
    string CheckId,
    Guid TenantId,
    string LocationId,
    string Status,
    DateTime Ts,
    double? LatencyMs,
    object? Http,
    object? Dns,
    object? Icmp,
    object? Tcp,
    string? Error,
    Dictionary<string, string>? Labels);