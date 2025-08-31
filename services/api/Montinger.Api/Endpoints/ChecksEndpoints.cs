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
            if (!new[] { "http", "dns", "icmp", "tcp" }.Contains(dto.Type))
                return Results.BadRequest(new { error = "invalid type" });

            var row = new Check
            {
                Id = string.IsNullOrWhiteSpace(dto.Id) ? NewUlid() : dto.Id,
                TenantId = dto.TenantId,
                Name = dto.Name,
                Type = dto.Type,
                Enabled = dto.Enabled,
                Schedule = dto.Schedule,
                Targets = JsonSerializer.Serialize(dto.Targets ?? new()),
                Settings = JsonSerializer.Serialize(dto.Settings ?? new()),
                Labels = JsonSerializer.Serialize(dto.Labels ?? new())
            };
            
            db.Checks.Add(row);
            await db.SaveChangesAsync();
            return Results.Created($"/v1/checks/{row.Id}", row);
        });

        g.MapGet("{id}", async (AppDb db, string id) =>
            await db.Checks.FindAsync(id) is { } found ? Results.Ok(found) : Results.NotFound());

        g.MapGet("",
            async (AppDb db, Guid? tenantId) => Results.Ok(await db.Checks
                .Where(c => tenantId == null || c.TenantId == tenantId).OrderByDescending(c => c.UpdatedAt).Take(200)
                .ToListAsync()));

        g.MapPut("{id}", async (AppDb db, string id, CheckUpdate dto) =>
        {
            var c = await db.Checks.FindAsync(id);
            if (c is null) return Results.NotFound();
            c.Name = dto.Name ?? c.Name;
            c.Enabled = dto.Enabled ?? c.Enabled;
            c.Schedule = dto.Schedule ?? c.Schedule;
            if(dto.Targets is not null) c.Targets = JsonSerializer.Serialize(dto.Targets);
            if(dto.Settings is not null) c.Settings = JsonSerializer.Serialize(dto.Settings);
            if(dto.Labels is not null) c.Labels = JsonSerializer.Serialize(dto.Labels);
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
            return Results.Ok();
        });

        return g;
    }
    
    static string NewUlid() => NUlid.Ulid.NewUlid().ToString();
}