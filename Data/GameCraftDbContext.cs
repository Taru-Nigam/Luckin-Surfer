using Microsoft.EntityFrameworkCore;
using GameCraft.Models;
using System.Collections.Generic;
using System.IO;
using System;

namespace GameCraft.Data
{
    public class GameCraftDbContext : DbContext
    {
        public GameCraftDbContext(DbContextOptions<GameCraftDbContext> options)
            : base(options)
        {
        }

        // DbSet properties for each table
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserType> UserTypes { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // --- NEW DBSETS ---
        public DbSet<RedeemedPrize> RedeemedPrizes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DailyTicketCollection> DailyTicketCollections { get; set; }
        // --- END NEW DBSETS ---

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserType>().HasKey(u => u.Id);
            modelBuilder.Entity<UserType>()
                .Property(u => u.Id)
                .ValueGeneratedNever();

            // Seed UserTypes (these are static and should remain)
            modelBuilder.Entity<UserType>().HasData(
                new UserType { Id = 0, Name = "Admin" },
                new UserType { Id = 1, Name = "User " },
                new UserType { Id = 2, Name = "Employee" }
            );

            // Seed Categories (these are static and should remain)
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Electronics" },
                new Category { CategoryId = 2, Name = "Toys" },
                new Category { CategoryId = 3, Name = "Accessories" },
                new Category { CategoryId = 4, Name = "Gaming" },
                new Category { CategoryId = 5, Name = "Gift Cards" }
            );

            // Seed Products (Prizes) - these are relatively static and should remain
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    Name = "High-End Wireless Earbuds",
                    Description = "Experience premium sound and ultimate freedom with our High-End Wireless Earbuds. Enjoy crystal-clear audio, comfortable fit, and intuitive controls for an immersive listening experience on the go.",
                    Price = 2200.00m,
                    CategoryId = 1,
                    ImageData = LoadImageData("wwwroot/images/prizes/highend wireless earbuds.jpg"),
                    Quantity = 20
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Jenga Boardgame",
                    Description = "Test your steady hand and strategic thinking with Jenga! Pull blocks from the tower and place them on top without making it tumble. A classic game of skill and suspense for all ages.",
                    Price = 1800.00m,
                    CategoryId = 4,
                    ImageData = LoadImageData("wwwroot/images/prizes/jenga boardgame.jpg"),
                    Quantity = 15
                },
                new Product
                {
                    ProductId = 3,
                    Name = "Plush Giant Bear",
                    Description = "Meet our incredibly soft and cuddly Plush Giant Bear! Perfect for big hugs and comforting snuggles, this lovable companion is ready to be your best friend. A timeless gift that brings joy to all ages.",
                    Price = 1500.00m,
                    CategoryId = 2,
                    ImageData = LoadImageData("wwwroot/images/prizes/plush giant bear.jpg"),
                    Quantity = 10
                },
                new Product
                {
                    ProductId = 4,
                    Name = "Multicable Charger",
                    Description = "Simplify your charging with our versatile Multicable Charger. Featuring multiple connectors, it's the perfect all-in-one solution to power up all your devices with just one cable.",
                    Price = 500.00m,
                    CategoryId = 1,
                    ImageData = LoadImageData("wwwroot/images/prizes/multicable charger.jpg"),
                    Quantity = 50
                },
                new Product
                {
                    ProductId = 5,
                    Name = "Stitch Keychain",
                    Description = "Carry a little bit of alien mischief with you everywhere! This adorable Stitch keychain features everyone's favorite mischievous blue alien, perfect for adding a touch of fun to your keys or bag.",
                    Price = 300.00m,
                    CategoryId = 2,
                    ImageData = LoadImageData("wwwroot/images/prizes/stitch keychain.jpeg"),
                    Quantity = 30
                },
                new Product
                {
                    ProductId = 6,
                    Name = "PS5 Console",
                    Description = "Dive into next-gen gaming with the PlayStation 5 console. Experience lightning-fast loading, immersive haptic feedback, adaptive triggers, and incredible 3D audio, bringing game worlds to life like never before.",
                    Price = 1000.00m,
                    CategoryId = 4,
                    ImageData = LoadImageData("wwwroot/images/prizes/PS5 console.jpg"),
                    Quantity = 5
                }
            );

                        // Load default avatar image data once
            byte[] defaultAvatarData = LoadImageData("wwwroot/images/default-avatar.png");
            if (defaultAvatarData == null)
            {
                // Handle case where default-avatar.png is not found
                // You might want to log an error or use a fallback empty byte array
                Console.WriteLine("Warning: default-avatar.png not found. Default avatars will be null.");
            }

            // Seed Customers (these are static initial users and should remain)
            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    CustomerId = 1,
                    Name = "Admin User",
                    Email = "admin@example.com",
                    Phone = "555-1234",
                    Address = "123 Admin St",
                    City = "AdminCity",
                    PostCode = "12345",
                    UserType = 0,
                    PasswordHash = "hashedpassword1", // Remember to actually hash passwords in a real app
                    Salt = "salt1", // Remember to generate unique salts in a real app
                    AvatarImageData = defaultAvatarData,
                    PrizePoints = 1000,
                    // AdminKey = "your_admin_db_key" // Add if you want a specific admin key in DB
                },
                new Customer
                {
                    CustomerId = 2,
                    Name = "Regular Customer",
                    Email = "customer@example.com",
                    Phone = "555-5678",
                    Address = "456 Customer Ave",
                    City = "CustomerTown",
                    PostCode = "67890",
                    UserType = 1,
                    PasswordHash = "hashedpassword2",
                    Salt = "salt2",
                    AvatarImageData = defaultAvatarData,
                    PrizePoints = 100,
                },
                new Customer
                {
                    CustomerId = 3,
                    Name = "Employee User",
                    Email = "employee@example.com",
                    Phone = "555-8765",
                    Address = "789 Employee Rd",
                    City = "EmployeeCity",
                    PostCode = "54321",
                    UserType = 2,
                    PasswordHash = "hashedpassword3",
                    Salt = "salt3",
                    AvatarImageData = defaultAvatarData,
                    PrizePoints = 500,
                }
            );

            // --- REMOVED SEED DATA FOR DYNAMIC TABLES (AuditLogs, RedeemedPrizes, DailyTicketCollections) ---
            // These tables should be populated by your application's runtime logic (e.g., controller actions)
            // if you want their data to be truly dynamic.
            // Keeping them in HasData with dynamic DateTime values causes the "model changes" error.
            // --- END REMOVED SEED DATA ---
        }

        // Helper method to load image data from file
        private byte[] LoadImageData(string filePath)
        {
            // Using Path.Combine for better cross-platform compatibility
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);

            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            // Optionally, log an error if the file is not found
            System.Diagnostics.Debug.WriteLine($"WARNING: Image file not found at {fullPath}");
            return null; // Return null if the file does not exist
        }
    }
}
