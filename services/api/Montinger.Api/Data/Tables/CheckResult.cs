using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Montinger.Api.Data.Tables;

public class CheckResult
{
    [Key] public string ResultId { get; set; } = default!;
    public string CheckId { get; set; } = default!;
    public Guid TenantId { get; set; }
    public string LocationId { get; set; } = default!;
    [MaxLength(12)] public string Status { get; set; } = "UNKNOWN"; // OK/WARN/CRIT/UNKNOWN
    public DateTime Ts { get; set; }
    public double? LatencyMs { get; set; }
    public JsonElement Payload { get; set; } = JsonSerializer.SerializeToElement(new { });
    public DateTime CreatedAt { get; set; }
    
}