using Microsoft.EntityFrameworkCore;
using GameCraft.Models;
using System.Collections.Generic;

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
        public DbSet<UserType> UserTypes { get; set; } // Only one declaration
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserType>().HasKey(u => u.Id); // Ensure Id is the primary key
            modelBuilder.Entity<UserType>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd(); // Configure Id to be auto-incremented
            // Seed data if necessary
            modelBuilder.Entity<UserType>().HasData(
                new UserType { Id = 1, Name = "Admin" },
                new UserType { Id = 2, Name = "User " },
                new UserType { Id = 3, Name = "Employee" }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Electronics" },
                new Category { CategoryId = 2, Name = "Toys" },
                new Category { CategoryId = 3, Name = "Accessories" },
                new Category { CategoryId = 4, Name = "Gaming" },
                new Category { CategoryId = 5, Name = "Gift Cards" }
            );

            // Seed Products (Prizes)
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    Name = "High-End Wireless Earbuds",
                    Description = "Experience premium sound and ultimate freedom with our High-End Wireless Earbuds. Enjoy crystal-clear audio, comfortable fit, and intuitive controls for an immersive listening experience on the go.",
                    Price = 2200.00m, // Changed from TicketCost to Price
                    CategoryId = 1, // Using CategoryId for Electronics
                    ImageUrl = "/images/prizes/highend wireless earbuds.jpg"
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Jenga Boardgame",
                    Description = "Test your steady hand and strategic thinking with Jenga! Pull blocks from the tower and place them on top without making it tumble. A classic game of skill and suspense for all ages.",
                    Price = 1800.00m, // Changed from TicketCost to Price
                    CategoryId = 4, // Using CategoryId for Gaming
                    ImageUrl = "/images/prizes/jenga boardgame.jpg"
                },
                new Product
                {
                    ProductId = 3,
                    Name = "Plush Giant Bear",
                    Description = "Meet our incredibly soft and cuddly Plush Giant Bear! Perfect for big hugs and comforting snuggles, this lovable companion is ready to be your best friend. A timeless gift that brings joy to all ages.",
                    Price = 1500.00m, // Changed from TicketCost to Price
                    CategoryId = 2, // Using CategoryId for Toys
                    ImageUrl = "/images/prizes/plush giant bear.jpg"
                },
                new Product
                {
                    ProductId = 4,
                    Name = "Multicable Charger",
                    Description = "Simplify your charging with our versatile Multicable Charger. Featuring multiple connectors, it's the perfect all-in-one solution to power up all your devices with just one cable.",
                    Price = 500.00m, // Changed from TicketCost to Price
                    CategoryId = 1, // Using CategoryId for Electronics
                    ImageUrl = "/images/prizes/multicable charger.jpg"
                },
                new Product
                {
                    ProductId = 5,
                    Name = "Stitch Keychain",
                    Description = "Carry a little bit of alien mischief with you everywhere! This adorable Stitch keychain features everyone's favorite mischievous blue alien, perfect for adding a touch of fun to your keys or bag.",
                    Price = 300.00m, // Changed from TicketCost to Price
                    CategoryId = 2, // Using CategoryId for Toys
                    ImageUrl = "/images/prizes/stitch keychain.jpeg"
                },
                new Product
                {
                    ProductId = 6,
                    Name = "PS5 Console",
                    Description = "Dive into next-gen gaming with the PlayStation 5 console. Experience lightning-fast loading, immersive haptic feedback, adaptive triggers, and incredible 3D audio, bringing game worlds to life like never before.",
                    Price = 1000.00m, // Changed from TicketCost to Price
                    CategoryId = 4, // Using CategoryId for Gaming
                    ImageUrl = "/images/prizes/PS5 console.jpg"
                }
            );


            // Seed Customers
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
                    UserType = 0, // Assuming this corresponds to the Admin UserType
                    PasswordHash = "hashedpassword1",
                    Salt = "salt1",
                    AvatarUrl = null,
                    PrizePoints = 1000,
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
                    AvatarUrl = null,
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
                    AvatarUrl = null,
                    PrizePoints = 500,
                }
            );
        }
    }
}
