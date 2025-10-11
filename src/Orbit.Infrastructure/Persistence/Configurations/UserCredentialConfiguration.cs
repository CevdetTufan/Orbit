using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Infrastructure.Persistence.Entities;

namespace Orbit.Infrastructure.Persistence.Configurations;

internal sealed class UserCredentialConfiguration : IEntityTypeConfiguration<UserCredentialEntity>
{
    public void Configure(EntityTypeBuilder<UserCredentialEntity> builder)
    {
        builder.ToTable("UserCredentials");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);
    }
}

