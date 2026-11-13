using BlueHeeler.Domain.Members;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlueHeeler.Api.Data;

/// <summary>The app's PostgreSQL database.</summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Member> Members => Set<Member>();

    /// <summary>Keys that encrypt the auth cookie, kept here so cookies survive restarts and scale-to-zero.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
