using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Navigation;

namespace Orbit.Infrastructure.Persistence.Configurations;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Url)
            .HasMaxLength(500);

        builder.Property(m => m.Description)
            .HasMaxLength(2000);

        builder.Property(m => m.Icon)
            .HasMaxLength(200);

        builder.Property(m => m.Order)
            .HasDefaultValue(0);

        builder.Property(m => m.Visible)
            .HasDefaultValue(true);

        // Self-referencing one-to-many: Parent -> Children
        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Permission relationship: Menu -> Permission (no navigation collection on Permission)
        // Use the strongly-typed FK property to avoid creating a shadow property
        builder.HasOne(m => m.Permission)
            .WithMany()
            .HasForeignKey(m => m.PermissionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(m => m.ParentId);
        builder.HasIndex(m => m.PermissionId);
    }
}
