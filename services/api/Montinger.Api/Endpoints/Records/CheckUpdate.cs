using System.Text.Json;

namespace Montinger.Api.Endpoints.Records;

public record CheckUpdate(
    string? Name,
    bool? Enabled,
    string? Schedule,
    List<string>? Targets,
    Dictionary<string, string>? Labels,
    JsonElement? Settings);