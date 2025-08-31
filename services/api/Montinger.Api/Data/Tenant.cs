using System.ComponentModel.DataAnnotations;

namespace Montinger.Api.Data;

public class Tenant
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}