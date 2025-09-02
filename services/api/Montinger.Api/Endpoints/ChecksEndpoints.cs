using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Montinger.Api.Data;
using Montinger.Api.Data.Tables;
using Montinger.Api.Endpoints.Records;

namespace Montinger.Api.Endpoints;

public static class ChecksEndpoints
{
    public static RouteGroupBuilder MapChecks(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1/checks");

        g.MapPost("", async (AppDb db, CheckCreate dto) =>
        {
            if (!new[] { "http","dns","icmp","tcp" }.Contains(dto.Type))
                return Results.BadRequest(new { error = "invalid type" });

            var row = new Check
            {
                Id = string.IsNullOrWhiteSpace(dto.Id) ? NUlid.Ulid.NewUlid().ToString() : dto.Id!,
                TenantId = dto.TenantId,
                Name = dto.Name,
                Type = dto.Type,
                Enabled = dto.Enabled,
                Schedule = dto.Schedule,
                Targets = dto.Targets is null
                    ? JsonSerializer.SerializeToElement(Array.Empty<string>())
                    : JsonSerializer.SerializeToElement(dto.Targets),
                Settings = dto.Settings ?? JsonSerializer.SerializeToElement(new { }),
                Labels = dto.Labels is null
                    ? JsonSerializer.SerializeToElement(new Dictionary<string, string>())
                    : JsonSerializer.SerializeToElement(dto.Labels)
            };

            db.Checks.Add(row);
            await db.SaveChangesAsync();
            return Results.Created($"/v1/checks/{row.Id}", row);
        });

        g.MapGet("{id}", async (AppDb db, string id) =>
            await db.Checks.FindAsync(id) is { } found ? Results.Ok(found) : Results.NotFound());

        g.MapGet("", async (AppDb db, Guid? tenantId) =>
        {
            var data = await db.Checks
                .Where(c => tenantId == null || c.TenantId == tenantId)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(200)
                .ToListAsync();

            return Results.Ok(data); // always JSON array ([])
        });

        g.MapPut("{id}", async (AppDb db, string id, CheckUpdate dto) =>
        {
            var c = await db.Checks.FindAsync(id);
            if (c is null) return Results.NotFound();

            c.Name = dto.Name ?? c.Name;
            if (dto.Enabled is not null) c.Enabled = dto.Enabled.Value;
            c.Schedule = dto.Schedule ?? c.Schedule;
            if (dto.Targets  is not null) c.Targets  = JsonSerializer.SerializeToElement(dto.Targets);
            if (dto.Settings is not null) c.Settings = dto.Settings.Value;
            if (dto.Labels is not null) c.Labels = JsonSerializer.SerializeToElement(dto.Labels);

            c.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(c);
        });

        g.MapDelete("{id}", async (AppDb db, string id) =>
        {
            var c = await db.Checks.FindAsync(id);
            if (c is null) return Results.NotFound();
            db.Checks.Remove(c);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return g;
    }
    
    static string NewUlid() => NUlid.Ulid.NewUlid().ToString();
}