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
    public DbSet<ChangesHistoryRecord> ChangesHistoryRecords => Set<ChangesHistoryRecord>(); 
    public DbSet<DirectoryGroup> DirectoryGroups => Set<DirectoryGroup>();
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

            entity.Property(e => e.DirectoryGroupId)
                .IsRequired();

            entity.HasMany(e => e.Fields)
                .WithOne(f => f.DirectoryType)
                .HasForeignKey(f => f.DirectoryTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DirectoryGroup)
                .WithMany()
                .HasForeignKey(e => e.DirectoryGroupId)
                .OnDelete(DeleteBehavior.Restrict);

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
        // DirectoryGroup configuration
        modelBuilder.Entity<DirectoryGroup>(entity =>
        {
            entity.ToTable("DirectoryGroups");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            // Seed stable default group so existing rows can point to it
            entity.HasData(new { Id = DirectoryGroup.DefaultId, Name = "Без группы" });
        });

        // ChangesHistoryRecords configuration
        modelBuilder.Entity<ChangesHistoryRecord>(entity =>
        {
            entity.ToTable("ChangesHistoryRecord");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.DirectoryTypeId)
                .IsRequired();

            entity.Property(e => e.RecordId)
                .IsRequired();

            entity.Property(e => e.TableName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.FieldName)
                .HasMaxLength(100);

            entity.Property(e => e.OldValue)
                .HasColumnType("text") 
                .IsRequired(false);

            entity.Property(e => e.NewValue)
                .HasColumnType("text") 
                .IsRequired(false);

            entity.Property(e => e.ChangedBy)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ChangedAt)
                .IsRequired();

            entity.HasOne(e => e.DirectoryType)
               .WithMany() 
               .HasForeignKey(e => e.DirectoryTypeId)
               .OnDelete(DeleteBehavior.Cascade); 

            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
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