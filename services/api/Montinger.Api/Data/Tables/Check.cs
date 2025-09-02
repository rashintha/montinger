using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Montinger.Api.Data.Tables;

public class Check
{
    [Key] public string Id { get; set; } = default!; // ULID/string
    public Guid TenantId { get; set; }
    [MaxLength(200)] public string Name { get; set; } = default!;
    [MaxLength(16)] public string Type { get; set; } = default!; // http,dns,icmp,tcp
    public bool Enabled { get; set; } = true;
    public string Schedule { get; set; } = default!; // cron-like
    public List<string> Targets { get; set; } = new();
    public JsonElement Settings { get; set; } = JsonSerializer.SerializeToElement(new { });
    public Dictionary<string, string> Labels { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}