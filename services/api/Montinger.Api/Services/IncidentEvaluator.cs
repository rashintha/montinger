using Microsoft.EntityFrameworkCore;
using Montinger.Api.Data;
using Montinger.Api.Data.Tables;

namespace Montinger.Api.Services;

public class IncidentEvaluator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IncidentEvaluator> _log;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(10);
    
    public IncidentEvaluator(IServiceScopeFactory scopeFactory, ILogger<IncidentEvaluator> log)
    {
        _scopeFactory = scopeFactory; _log = log;
    }
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(Period);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await EvaluateOnce(ct); }
            catch (Exception ex) { _log.LogError(ex, "Incident evaluation failed"); }
        }
    }
    
    private async Task EvaluateOnce(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        // Get recent checks that had activity in last 5 minutes
        var since = DateTime.UtcNow.AddMinutes(-5);
        var recentCheckIds = await db.CheckResults
            .Where(r => r.Ts >= since)
            .Select(r => r.CheckId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var checkId in recentCheckIds)
        {
            var last3 = await db.CheckResults
                .Where(r => r.CheckId == checkId)
                .OrderByDescending(r => r.Ts)
                .Take(3)
                .Select(r => new { r.Status, r.TenantId })
                .ToListAsync(ct);

            if (last3.Count < 3) continue;

            bool allCrit = last3.All(r => r.Status == "CRIT");
            bool allOk   = last3.All(r => r.Status == "OK");
            var tenantId = last3.First().TenantId;

            var open = await db.Incidents
                .Where(i => i.CheckId == checkId && i.IsOpen)
                .FirstOrDefaultAsync(ct);

            if (allCrit)
            {
                if (open is null)
                {
                    db.Incidents.Add(new Incident {
                        CheckId = checkId, TenantId = tenantId,
                        Severity = "critical", IsOpen = true,
                        Summary = "3 consecutive CRIT results"
                    });
                    await db.SaveChangesAsync(ct);
                }
            }
            else if (allOk && open is not null)
            {
                open.IsOpen = false;
                open.ResolvedAt = DateTime.UtcNow;
                open.Summary = "Recovered: 3 consecutive OK results";
                await db.SaveChangesAsync(ct);
            }
        }
    }
}