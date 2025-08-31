namespace Montinger.Api.Endpoints.Records;

public record CheckCreate(
    string? Id,
    Guid TenantId,
    string Name,
    string Type,
    bool Enabled,
    string Schedule,
    List<string>? Targets,
    Dictionary<string, string>? Labels,
    object? Settings);