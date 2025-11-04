using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DatabookService.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DirectoryType> DirectoryTypes => Set<DirectoryType>();
    public DbSet<DirectoryField> DirectoryFields => Set<DirectoryField>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DirectoryType configuration
        modelBuilder.Entity<DirectoryType>(entity =>
        {
            entity.ToTable("DirectoryTypes");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.TableName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(e => e.TableName)
                .IsUnique();
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.HasMany(e => e.Fields)
                .WithOne(f => f.DirectoryType)
                .HasForeignKey(f => f.DirectoryTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DirectoryField configuration
        modelBuilder.Entity<DirectoryField>(entity =>
        {
            entity.ToTable("DirectoryFields");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.ColumnName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.DataType)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.IsRequired)
                .IsRequired();
            
            entity.Property(e => e.Order)
                .IsRequired();
            
            entity.HasOne(e => e.ReferenceDirectoryType)
                .WithMany()
                .HasForeignKey(e => e.ReferenceDirectoryTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ApiKey configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.KeyHash)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => new { e.KeyHash, e.IsActive });
        });
    }
}