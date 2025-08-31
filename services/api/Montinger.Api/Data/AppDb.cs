using Microsoft.EntityFrameworkCore;

namespace Montinger.Api.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
    
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Check> Checks => Set<Check>();
    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        
        b.Entity<Check>().HasIndex(x => x.TenantId);
        b.Entity<Check>().Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        b.Entity<Check>().Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
        b.Entity<Check>().Property(x => x.Labels).HasColumnType("jsonb");
        b.Entity<Check>().Property(x => x.Targets).HasColumnType("jsonb");
        b.Entity<Check>().Property(x => x.Settings).HasColumnType("jsonb");
        
        b.Entity<CheckResult>().HasIndex(x => new {x.CheckId, x.Ts}).HasDatabaseName("ix_check_result_ts");
        b.Entity<CheckResult>().Property(x => x.Payload).HasColumnType("jsonb");
        b.Entity<CheckResult>().Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
    }
}