using Microsoft.EntityFrameworkCore;
using StockFlow.Web.Models;
using System.Security.Cryptography;
using System.Text;

namespace StockFlow.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await SeedUsersAsync(db);
            await SeedItemsAsync(db);
            await SeedShipmentsAsync(db);
        }

        private static async Task SeedUsersAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync()) return;

            var users = new List<User>
            {
                new User
                {
                    FullName = "System Admin",
                    Email = "admin@stockflow.com",
                    PasswordHash = HashPassword("Admin@1234"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FullName = "Warehouse Manager",
                    Email = "manager@stockflow.com",
                    PasswordHash = HashPassword("Manager@1234"),
                    Role = "Manager",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FullName = "Warehouse Staff",
                    Email = "staff@stockflow.com",
                    PasswordHash = HashPassword("Staff@1234"),
                    Role = "Staff",
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.Users.AddRange(users);
            await db.SaveChangesAsync();
        }

        private static async Task SeedItemsAsync(AppDbContext db)
        {
            if (await db.Items.AnyAsync()) return;

            var adminId = await db.Users
                .Where(u => u.Email == "admin@stockflow.com")
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            var items = new List<Item>
            {
                new Item { ItemName = "Basmati Rice",     SKU = "RICE-001", Unit = "kg",  IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Wheat Flour",      SKU = "WFL-004",  Unit = "kg",  IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Refined Sugar",    SKU = "SUG-002",  Unit = "kg",  IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Iodised Salt",     SKU = "SLT-009",  Unit = "kg",  IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Corn Starch",      SKU = "CRN-007",  Unit = "kg",  IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Sunflower Oil",    SKU = "OIL-003",  Unit = "ltr", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Black Pepper",     SKU = "PEP-011",  Unit = "g",   IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Turmeric Powder",  SKU = "TRM-005",  Unit = "g",   IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Packaging Box 5kg",SKU = "PKG-5KG",  Unit = "pcs", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId },
                new Item { ItemName = "Packaging Box 2kg",SKU = "PKG-2KG",  Unit = "pcs", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = adminId }
            };

            db.Items.AddRange(items);
            await db.SaveChangesAsync();
        }

        private static async Task SeedShipmentsAsync(AppDbContext db)
        {
            if (await db.Shipments.AnyAsync()) return;

            var staffId = await db.Users
                .Where(u => u.Email == "staff@stockflow.com")
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            var riceId   = await db.Items.Where(i => i.SKU == "RICE-001").Select(i => i.ItemId).FirstOrDefaultAsync();
            var flourId  = await db.Items.Where(i => i.SKU == "WFL-004").Select(i => i.ItemId).FirstOrDefaultAsync();
            var sugarId  = await db.Items.Where(i => i.SKU == "SUG-002").Select(i => i.ItemId).FirstOrDefaultAsync();
            var saltId   = await db.Items.Where(i => i.SKU == "SLT-009").Select(i => i.ItemId).FirstOrDefaultAsync();
            var cornId   = await db.Items.Where(i => i.SKU == "CRN-007").Select(i => i.ItemId).FirstOrDefaultAsync();

            var shipments = new List<Shipment>
            {
                new Shipment { ItemId = riceId,  TotalWeight = 500,  Status = "Pending",    ReceivedAt = DateTime.UtcNow.AddDays(-1),  ReceivedBy = staffId },
                new Shipment { ItemId = flourId, TotalWeight = 1200, Status = "InProgress", ReceivedAt = DateTime.UtcNow.AddDays(-2),  ReceivedBy = staffId },
                new Shipment { ItemId = sugarId, TotalWeight = 800,  Status = "Pending",    ReceivedAt = DateTime.UtcNow.AddDays(-3),  ReceivedBy = staffId },
                new Shipment { ItemId = saltId,  TotalWeight = 2000, Status = "Processed",  ReceivedAt = DateTime.UtcNow.AddDays(-5),  ReceivedBy = staffId },
                new Shipment { ItemId = cornId,  TotalWeight = 400,  Status = "Pending",    ReceivedAt = DateTime.UtcNow.AddDays(-26), ReceivedBy = staffId }
            };

            db.Shipments.AddRange(shipments);
            await db.SaveChangesAsync();

            await SeedProcessedItemsAsync(db, staffId);
        }

        private static async Task SeedProcessedItemsAsync(AppDbContext db, int staffId)
        {
            var managerId = await db.Users
                .Where(u => u.Email == "manager@stockflow.com")
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            var saltShipment = await db.Shipments
                .Include(s => s.Item)
                .FirstOrDefaultAsync(s => s.Item != null && s.Item.SKU == "SLT-009");

            var saltItemId = await db.Items
                .Where(i => i.SKU == "SLT-009")
                .Select(i => i.ItemId)
                .FirstOrDefaultAsync();

            var pkg5Id = await db.Items
                .Where(i => i.SKU == "PKG-5KG")
                .Select(i => i.ItemId)
                .FirstOrDefaultAsync();

            var pkg2Id = await db.Items
                .Where(i => i.SKU == "PKG-2KG")
                .Select(i => i.ItemId)
                .FirstOrDefaultAsync();

            if (saltShipment == null) return;

            var child1 = new ProcessedItem
            {
                ParentId = null,
                ItemId = pkg5Id,
                ShipmentId = saltShipment.ShipmentId,
                InputWeight = 2000,
                OutputWeight = 1000,
                Status = "Approved",
                ProcessedAt = DateTime.UtcNow.AddDays(-4),
                ProcessedBy = staffId
            };

            var child2 = new ProcessedItem
            {
                ParentId = null,
                ItemId = pkg2Id,
                ShipmentId = saltShipment.ShipmentId,
                InputWeight = 2000,
                OutputWeight = 800,
                Status = "Approved",
                ProcessedAt = DateTime.UtcNow.AddDays(-4),
                ProcessedBy = staffId
            };

            db.ProcessedItems.AddRange(child1, child2);
            await db.SaveChangesAsync();

            var grandchild1 = new ProcessedItem
            {
                ParentId = child1.ProcessedItemId,
                ItemId = pkg5Id,
                ShipmentId = saltShipment.ShipmentId,
                InputWeight = 1000,
                OutputWeight = 400,
                Status = "Approved",
                ProcessedAt = DateTime.UtcNow.AddDays(-3),
                ProcessedBy = staffId
            };

            var grandchild2 = new ProcessedItem
            {
                ParentId = child1.ProcessedItemId,
                ItemId = pkg2Id,
                ShipmentId = saltShipment.ShipmentId,
                InputWeight = 1000,
                OutputWeight = 500,
                Status = "Pending",
                ProcessedAt = DateTime.UtcNow.AddDays(-3),
                ProcessedBy = staffId
            };

            db.ProcessedItems.AddRange(grandchild1, grandchild2);
            await db.SaveChangesAsync();

            await db.AuditLogs.AddRangeAsync(new List<AuditLog>
            {
                new AuditLog { EntityName = "ProcessedItem", EntityId = child1.ProcessedItemId, Action = "Process",  PerformedBy = staffId,   Details = "Seeded",  PerformedAt = DateTime.UtcNow.AddDays(-4) },
                new AuditLog { EntityName = "ProcessedItem", EntityId = child1.ProcessedItemId, Action = "Approve",  PerformedBy = managerId, Details = "Seeded",  PerformedAt = DateTime.UtcNow.AddDays(-4) },
                new AuditLog { EntityName = "ProcessedItem", EntityId = child2.ProcessedItemId, Action = "Process",  PerformedBy = staffId,   Details = "Seeded",  PerformedAt = DateTime.UtcNow.AddDays(-4) },
                new AuditLog { EntityName = "ProcessedItem", EntityId = child2.ProcessedItemId, Action = "Approve",  PerformedBy = managerId, Details = "Seeded",  PerformedAt = DateTime.UtcNow.AddDays(-4) }
            });

            await db.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "StockFlow_Salt_2025"));
            return Convert.ToBase64String(bytes);
        }
    }
}