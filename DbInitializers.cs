using GameCraft.Data;
using GameCraft.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.DbInitializers
{
    public static class DbInitializer
    {
        public static async Task Initialize(GameCraftDbContext context)
        {
            // Ensure the database is created and migrations are applied
            // This is good practice to ensure the schema is ready before seeding
            await context.Database.MigrateAsync();

            // Seed AuditLogs
            if (!await context.AuditLogs.AnyAsync()) // Only seed if no audit logs exist
            {
                Console.WriteLine("Seeding initial AuditLogs...");
                context.AuditLogs.AddRange(
                    new AuditLog { Action = "System Initialization", EmployeeName = "System", Timestamp = DateTime.UtcNow.AddDays(-3) },
                    new AuditLog { Action = "First employee login (seed)", EmployeeName = "Employee User", Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-5) },
                    new AuditLog { Action = "Prize stock updated (seed)", EmployeeName = "Employee User", Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-1) },
                    new AuditLog { Action = "Admin logged in (initial seed)", EmployeeName = "Admin", Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-3) }
                );
                await context.SaveChangesAsync();
                Console.WriteLine("AuditLogs seeded.");
            }
            else
            {
                Console.WriteLine("AuditLogs already contain data. Skipping seed.");
            }

            // Seed RedeemedPrizes (ensure Products and Customers exist before running this)
            if (!await context.RedeemedPrizes.AnyAsync()) // Only seed if no redeemed prizes exist
            {
                // You might need to retrieve ProductId and CustomerId dynamically if you don't know their IDs
                // For this example, let's assume ProductId 1 and CustomerId 2 exist from your static seeding.
                var product1 = await context.Products.FirstOrDefaultAsync(p => p.Name == "High-End Wireless Earbuds");
                var product5 = await context.Products.FirstOrDefaultAsync(p => p.Name == "Stitch Keychain");
                var customer2 = await context.Customers.FirstOrDefaultAsync(c => c.Name == "Regular Customer");

                if (product1 != null && product5 != null && customer2 != null)
                {
                    Console.WriteLine("Seeding initial RedeemedPrizes...");
                    context.RedeemedPrizes.AddRange(
                        new RedeemedPrize { ProductId = product1.ProductId, CustomerId = customer2.CustomerId, TicketsSpent = 2200, RedemptionDate = DateTime.UtcNow.AddDays(-1).AddHours(-6), EmployeeName = "Employee User" },
                        new RedeemedPrize { ProductId = product5.ProductId, CustomerId = customer2.CustomerId, TicketsSpent = 300, RedemptionDate = DateTime.UtcNow.AddHours(-8), EmployeeName = "Employee User" }
                    );
                    await context.SaveChangesAsync();
                    Console.WriteLine("RedeemedPrizes seeded.");
                }
                else
                {
                    Console.WriteLine("Skipping RedeemedPrizes seed: Required Product/Customer data not found.");
                }
            }
            else
            {
                Console.WriteLine("RedeemedPrizes already contain data. Skipping seed.");
            }

            // Seed DailyTicketCollections
            if (!await context.DailyTicketCollections.AnyAsync()) // Only seed if no daily collections exist
            {
                Console.WriteLine("Seeding initial DailyTicketCollections...");
                context.DailyTicketCollections.AddRange(
                    new DailyTicketCollection { CollectionDate = DateTime.Today.AddDays(-2), TotalTicketsCollected = 9500 },
                    new DailyTicketCollection { CollectionDate = DateTime.Today.AddDays(-1), TotalTicketsCollected = 14000 },
                    new DailyTicketCollection { CollectionDate = DateTime.Today, TotalTicketsCollected = 7800 } // Data for "today"
                );
                await context.SaveChangesAsync();
                Console.WriteLine("DailyTicketCollections seeded.");
            }
            else
            {
                Console.WriteLine("DailyTicketCollections already contain data. Skipping seed.");
            }

            Console.WriteLine("Database initialization complete.");
        }
    }
}