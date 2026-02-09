using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using Microsoft.EntityFrameworkCore;

namespace LongJobProcessor.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var jobEntity = modelBuilder.Entity<Job>();

        jobEntity.HasKey(j => j.Id);

        jobEntity.Property(j => j.UserId)
                 .IsRequired()
                 .HasMaxLength(128);

        jobEntity.Property(j => j.CreatedAt)
                 .IsRequired();

        jobEntity.HasDiscriminator<string>("JobType")
                 .HasValue<InputEncodeJob>("InputEncode");

        jobEntity.OwnsOne(j => j.State, sm =>
        {
            sm.Property(s => s.Status)
              .HasConversion<string>()
              .HasMaxLength(32)
              .HasColumnName("Status")
              .IsRequired();

            sm.Property(s => s.JobOwner)
              .HasMaxLength(128)
              .HasColumnName("JobOwner")
              .IsRequired(false);

            sm.Property(s => s.TakenUntil)
              .HasColumnName("TakenUntil")
              .IsRequired(false);

            sm.Property(s => s.RetryCount)
              .HasColumnName("RetryCount")
              .HasDefaultValue(0);
        });

        modelBuilder.Entity<InputEncodeJob>(entity =>
        {
            entity.Property(j => j.Input)
                  .IsRequired()
                  .HasMaxLength(4000);

            entity.Property(j => j.Cursor)
                  .HasDefaultValue(0);

            entity.Property(j => j.Produced)
                  .HasMaxLength(8000);
        });
    }
}
