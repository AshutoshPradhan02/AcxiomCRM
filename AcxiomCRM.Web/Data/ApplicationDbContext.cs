using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<CrmActivity> Activities => Set<CrmActivity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();
        builder.Entity<Customer>()
            .HasIndex(c => c.Phone)
            .IsUnique();
        builder.Entity<Customer>()
            .HasIndex(c => c.CustomerCode)
            .IsUnique();

        builder.Entity<Lead>()
            .HasIndex(l => l.LeadCode)
            .IsUnique();

        builder.Entity<Customer>()
            .Property(c => c.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Entity<Lead>()
            .Property(l => l.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Entity<Opportunity>()
            .Property(o => o.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Entity<AuditLog>()
            .Property(a => a.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<Opportunity>()
            .Property(o => o.Amount)
            .HasPrecision(18, 2);
        builder.Entity<Lead>()
            .Property(l => l.ExpectedValue)
            .HasPrecision(18, 2);
    }
}
