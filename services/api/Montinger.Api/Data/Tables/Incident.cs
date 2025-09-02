using System.ComponentModel.DataAnnotations;

namespace Montinger.Api.Data.Tables;

public class Incident
{
    [Key] public string Id { get; set; } = NUlid.Ulid.NewUlid().ToString();
    public string CheckId { get; set; } = default!;
    public Guid TenantId { get; set; }
    [MaxLength(16)] public string Severity { get; set; } = "critical"; // critical/warn
    public bool IsOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    [MaxLength(500)] public string? Summary { get; set; }
}