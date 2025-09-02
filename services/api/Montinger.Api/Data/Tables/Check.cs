using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Montinger.Api.Data.Tables;

public class Check
{
    [Key] public string Id { get; set; } = default!; // ULID/string
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!; // http,dns,icmp,tcp
    public bool Enabled { get; set; } = true;
    public string Schedule { get; set; } = default!; // cron-like
    public JsonElement Targets { get; set; }
    public JsonElement Settings { get; set; }
    public JsonElement Labels { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}