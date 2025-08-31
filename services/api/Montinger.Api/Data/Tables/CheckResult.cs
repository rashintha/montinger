using System.ComponentModel.DataAnnotations;

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
    public string Payload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    
}