using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DailyWatt.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<DailyWattUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<EnedisCredential> EnedisCredentials => Set<EnedisCredential>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<WeatherDay> WeatherDays => Set<WeatherDay>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EnedisCredential>(b =>
        {
            b.HasKey(x => x.UserId);
            b.HasOne(x => x.User)
                .WithMany(u => u.EnedisCredentials)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Property(x => x.LoginEncrypted).IsRequired();
            b.Property(x => x.PasswordEncrypted).IsRequired();
            b.Property(x => x.MeterNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
        });

        builder.Entity<Measurement>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.TimestampUtc });
            b.Property(x => x.Source).HasMaxLength(64);
            b.HasOne(x => x.User)
                .WithMany(u => u.Measurements)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        var dateConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            d => DateOnly.FromDateTime(DateTime.SpecifyKind(d, DateTimeKind.Utc)));

        builder.Entity<WeatherDay>(b =>
        {
            b.HasKey(x => new { x.UserId, x.Date });
            b.Property(x => x.Date)
                .HasConversion(dateConverter)
                .IsRequired();
            b.Property(x => x.TempAvg).IsRequired();
            b.Property(x => x.TempMin).IsRequired();
            b.Property(x => x.TempMax).IsRequired();
            b.Property(x => x.Source).HasMaxLength(64).IsRequired();
            b.Property(x => x.Latitude).IsRequired();
            b.Property(x => x.Longitude).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(u => u.WeatherDays)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ImportJob>(b =>
        {
            b.Property(x => x.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ImportJobStatus>(v))
                .HasMaxLength(32);
            b.Property(x => x.ErrorCode).HasMaxLength(128);
            b.Property(x => x.ErrorMessage).HasMaxLength(1024);
            b.HasOne(x => x.User)
                .WithMany(u => u.ImportJobs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.Status, x.CreatedAt });
        });
    }
}
