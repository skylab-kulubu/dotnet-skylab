using Skylab.Feedbacks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Skylab.Feedbacks.Infrastructure.Storage.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedbacks");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Content).IsRequired();

        builder.Property(f => f.Topic).IsRequired();

        builder.Property(f => f.Source).IsRequired();
    }
}