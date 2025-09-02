using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Montinger.Api.Data;
using Montinger.Api.Data.Tables;
using Montinger.Api.Endpoints.Records;

namespace Montinger.Api.Endpoints;

public static class ResultsEndpoints
{
    public static RouteGroupBuilder MapResults(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1/results");

        g.MapPost("", async (AppDb db, ResultIn r) =>
        {
            var exists = await db.Checks.AnyAsync(c => c.Id == r.CheckId && c.TenantId == r.TenantId);
            if (!exists) return Results.BadRequest(new { error = "unknown check/tenant" });

            var payload = JsonSerializer.SerializeToElement(new { r.Http, r.Dns, r.Icmp, r.Tcp, r.Error, r.Labels });


            var row = new CheckResult {
                ResultId = string.IsNullOrWhiteSpace(r.ResultId) ? NUlid.Ulid.NewUlid().ToString() : r.ResultId!,
                CheckId = r.CheckId,
                TenantId = r.TenantId,
                LocationId = r.LocationId,
                Status = r.Status,
                Ts = r.Ts,
                LatencyMs = r.LatencyMs,
                Payload = payload
            };

            db.CheckResults.Add(row);
            await db.SaveChangesAsync();
            return Results.Accepted(null, new { row.ResultId });
        });

        return g;
    }
}