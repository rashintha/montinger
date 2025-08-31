using System.ComponentModel.DataAnnotations;

namespace Montinger.Api.Data;

public class Check
{
    [Key] public string Id { get; set; } = default!; // ULID/string
    public Guid TenantId { get; set; }
    [MaxLength(200)] public string Name { get; set; } = default!;
    [MaxLength(16)] public string Type { get; set; } = default!; // http,dns,icmp,tcp
    public bool Enabled { get; set; } = true;
    public string Schedule { get; set; } = default!; // cron-like
    public string Targets { get; set; } = "[]"; // jsonb
    public string Settings { get; set; } = "{}"; // jsonb
    public string Labels { get; set; } = "{}"; // jsonb
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}