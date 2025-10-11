using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Security;

namespace Orbit.Infrastructure.Persistence.Configurations;

internal sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.UserId)
            .IsRequired(false);

        builder.Property(x => x.AttemptedAtUtc)
            .IsRequired();

        builder.Property(x => x.IsSuccessful)
            .IsRequired();

        builder.Property(x => x.RemoteIp)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);
    }
}

