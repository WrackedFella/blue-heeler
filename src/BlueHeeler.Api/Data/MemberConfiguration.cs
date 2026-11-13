using BlueHeeler.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueHeeler.Api.Data;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);

        // Stored already normalised, so a plain unique index enforces one member per address.
        builder.Property(m => m.Email)
            .IsRequired()
            .HasMaxLength(254)
            .HasConversion(e => e.Value, v => FromStored(v));
        builder.HasIndex(m => m.Email).IsUnique();

        // Text, not integers, so backups and the Neon console stay readable.
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.Tier).HasColumnName("walker_tier");
        builder.HasIndex(m => m.Role);
        builder.Property(m => m.CreatedAt);
    }

    private static EmailAddress FromStored(string value) =>
        EmailAddress.TryCreate(value) ?? throw new InvalidOperationException("A stored member email failed validation.");
}
