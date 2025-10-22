using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orbit.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired();

        builder.Property(e => e.Payload)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();
    }
}