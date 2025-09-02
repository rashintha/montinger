using Microsoft.EntityFrameworkCore;
using Montinger.Api.Data;

namespace Montinger.Api.Endpoints;

public static class IncidentsEndpoints
{
    public static RouteGroupBuilder MapIncidents(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1/incidents");

        g.MapGet("", async (AppDb db, Guid? tenantId, bool? open) =>
        {
            var q = db.Incidents.AsQueryable();
            if (tenantId is not null) q = q.Where(i => i.TenantId == tenantId);
            if (open is not null) q = q.Where(i => i.IsOpen == open);
            var rows = await q.OrderByDescending(i => i.CreatedAt).Take(200).ToListAsync();
            return Results.Ok(rows);
        });

        return g;
    }
}