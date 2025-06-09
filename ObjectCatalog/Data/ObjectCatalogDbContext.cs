using Microsoft.EntityFrameworkCore;
using ObjectCatalog.Models;

namespace ObjectCatalog.Data;

public class ObjectCatalogDbContext : DbContext
{
    public ObjectCatalogDbContext(DbContextOptions<ObjectCatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<ObjectEntity> Objects { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ObjectCategory> ObjectCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObjectEntity>(entity =>
        {            
            entity.HasIndex(t => new { t.Name });
        });

        modelBuilder.Entity<ObjectEntity>()
            .HasMany(o => o.Categories)
            .WithMany(c => c.Objects)            
            .UsingEntity<ObjectCategory>(
                j => j
                    .HasOne(oc => oc.Category)
                    .WithMany()
                    .HasForeignKey(oc => oc.CategoryId),
                j => j
                    .HasOne(oc => oc.Object)
                    .WithMany()
                    .HasForeignKey(oc => oc.ObjectId),
                j =>
                {
                    j.HasKey(oc => new { oc.ObjectId, oc.CategoryId });
                });

        modelBuilder.Entity<ObjectEntity>()
            .Property(o => o.Price)            
            .HasPrecision(18, 2);
    }
}