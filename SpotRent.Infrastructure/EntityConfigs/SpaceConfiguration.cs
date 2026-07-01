using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpotRent.Domain.Entities;

namespace SpotRent.Infrastructure.EntityConfigs;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.SpaceType)
            .IsRequired();

        builder.Property(s => s.Capacity)
            .IsRequired();
        
        builder.Property(s => s.AreaSqm)
            .IsRequired();
        
        builder.Property(s => s.Room)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(s => s.HourlyRate)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.OwnerId)
            .IsRequired();

        builder.Property(s => s.AddressId)
            .IsRequired();
        
        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.Space)
            .HasForeignKey(b => b.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.WorkingHours)
            .WithOne(wh => wh.Space)
            .HasForeignKey(wh => wh.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(s => s.AttributeValues)
            .WithOne(av => av.Space)
            .HasForeignKey(av => av.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(s => s.Owner)
            .WithMany(u => u.Spaces)
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Address)
            .WithMany(a => a.Spaces)
            .HasForeignKey(s => s.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
