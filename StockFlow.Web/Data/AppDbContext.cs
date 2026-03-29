using Microsoft.EntityFrameworkCore;
using StockFlow.Web.Models;

namespace StockFlow.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ProcessedItem> ProcessedItems => Set<ProcessedItem>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProcessedItem>()
                .HasOne(p => p.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcessedItem>()
                .HasOne(p => p.Item)
                .WithMany()
                .HasForeignKey(p => p.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcessedItem>()
                .HasOne(p => p.Shipment)
                .WithMany()
                .HasForeignKey(p => p.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Item)
                .WithMany()
                .HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.ReceivedByUser)
                .WithMany()
                .HasForeignKey(s => s.ReceivedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Item>()
                .HasIndex(i => i.SKU)
                .IsUnique();

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityName, a.EntityId });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.PerformedBy);

            modelBuilder.Entity<ProcessedItem>()
                .HasIndex(p => p.ShipmentId);

            modelBuilder.Entity<ProcessedItem>()
                .HasIndex(p => p.ParentId);

            modelBuilder.Entity<Shipment>()
                .HasIndex(s => s.Status);
        }
    }
}