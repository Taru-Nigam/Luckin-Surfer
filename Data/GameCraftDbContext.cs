using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using GameCraft.Helpers;
using GameCraft.Models;

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

        public DbSet<RedeemedPrize> RedeemedPrizes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DailyTicketCollection> DailyTicketCollections { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        public DbSet<Icon> Icons { get; set; }
        public DbSet<Logo> Logos { get; set; }
        public DbSet<Card> Cards { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed initial data for Logos
            modelBuilder.Entity<Logo>().HasData(
                new Logo
                {
                    LogoId = 1,
                    Name = "GameCraft Logo",
                    Description = "Main website logo",
                    ImageData = LoadImageData("wwwroot/images/GameCraft logo.png") // Load image data
                },
                new Logo
                {
                    LogoId = 2,
                    Name = "Cart Icon",
                    Description = "Shopping cart icon",
                    ImageData = LoadImageData("wwwroot/images/cart icon.png") // Load image data
                }
            );

            // Seed initial data for Cards
            modelBuilder.Entity<Card>().HasData(
            new Card
            {
                CardId = 1,
                Name = "GameCraft Card",
                Price = 10.00m,
                Description = "- Seamless Ticket Tracking: All your game tickets are digitally stored on your card.\n- Exclusive Member Perks: Access special discounts and offers available only to cardholders.\n- Earn & Redeem Points: Accumulate prize points with every game and redeem them for awesome rewards.\n- Early Access & Invites: Get priority access to new games, events, and exclusive tournaments.\n- Level Up Your Fun: The more you play, the more rewards and status you unlock!",
                ImageData = LoadImageData("wwwroot/images/GameCard card.png") // Load image data
            },
                new Card
                {
                    CardId = 2,
                    Name = "Silver GameCraft Card",
                    Price = 20.00m,
                    Description = "- 300 Prize Points: Start your journey with 300 prize points.\n- Special Discounts: Enjoy exclusive discounts on selected games and products.\n- Early Access: Get early access to new games and events.",
                    ImageData = LoadImageData("wwwroot/images/GameCard card.png") // Load image data
                },
                 new Card
                 {
                     CardId = 3,
                     Name = "Gold GameCraft Card",
                     Price = 40.00m,
                     Description = "- 800 Prize Points: Start your journey with 800 prize points.\n- Premium Discounts: Enjoy premium discounts on all games and products.\n- VIP Access: Get VIP access to exclusive events and tournaments.\n- Priority Support: Receive priority customer support for all your inquiries.",
                     ImageData = LoadImageData("wwwroot/images/GameCard plat.png") // Load image data
                 }
            );

            // Seed initial data for Icons (example)
            modelBuilder.Entity<Icon>().HasData(
                new Icon { IconId = 1, Name = "Play & Earn", Description = "Win Tickets playing games!", ImageData = File.ReadAllBytes("wwwroot/images/play&earn icon.png"), Order = 1 },
                new Icon { IconId = 2, Name = "Connect Account", Description = "Link your arcade ID online", ImageData = File.ReadAllBytes("wwwroot/images/link icon.png"), Order = 2 },
                new Icon { IconId = 3, Name = "Browse Prizes", Description = "See all cool rewards.", ImageData = File.ReadAllBytes("wwwroot/images/prize icon.png"), Order = 3 },
                new Icon { IconId = 4, Name = "Redeem", Description = "Choose and claim your prize!", ImageData = File.ReadAllBytes("wwwroot/images/redeem icon.png"), Order = 4 }
            );

            // Seed initial data for Promotions (ONLY THE FIRST TWO ARE KEPT)
            // IMPORTANT: Replaced DateTime.UtcNow with static DateTime values
            modelBuilder.Entity<Promotion>().HasData(
                new Promotion
                {
                    PromotionId = 1,
                    Title = "Sign Up & Get 500 Bonus Tickets!",
                    Description = "Join GameCraft today and kickstart your rewards with 500 FREE tickets!",
                    ImageData = File.ReadAllBytes("wwwroot/images/Bonus-tickets.png"),
                    ButtonText = "Register Now",
                    ButtonUrl = "/Account/SpecialRegister",
                    BackgroundColor = "#FFD700", // Gold-like color
                    TextColor = "#333333", // Dark text for contrast
                },
                new Promotion
                {
                    PromotionId = 2,
                    Title = "Purchase Your GameCraft Card!",
                    Description = "Get your official GameCraft card for seamless ticket tracking and exclusive perks!",
                    ImageData = File.ReadAllBytes("wwwroot/images/GameCard card.png"),
                    ButtonText = "Get Your Card",
                    ButtonUrl = "Card/Details/1",
                    BackgroundColor = "#00BFFF", // Deep Sky Blue
                    TextColor = "#FFFFFF",
                },
                new Promotion
                {
                    PromotionId = 3,
                    Title = "Purchase Your Silver GameCraft Card!",
                    Description = "Get your Silver GameCraft card for exclusive perks!",
                    ImageData = File.ReadAllBytes("wwwroot/images/GameCard card.png"),
                    ButtonText = "Get Your Silver Card",
                    ButtonUrl = "Card/Details/2",
                    BackgroundColor = "#C0C0C0", // Silver
                    TextColor = "#000000",
                },
                new Promotion
                {
                    PromotionId = 4,
                    Title = "Purchase Your Gold GameCraft Card!",
                    Description = "Get your Gold GameCraft card for the ultimate experience!",
                    ImageData = File.ReadAllBytes("wwwroot/images/GameCard plat.png"),
                    ButtonText = "Get Your Gold Card",
                    ButtonUrl = "Card/Details/3",
                    BackgroundColor = "#FFD700", // Gold
                    TextColor = "#000000",
                }
            );


        modelBuilder.Entity<UserType>().HasKey(u => u.Id);
            modelBuilder.Entity<UserType>()
                .Property(u => u.Id)
                .ValueGeneratedNever();

            // Seed UserTypes
            modelBuilder.Entity<UserType>().HasData(
                new UserType { Id = 0, Name = "Admin" },
                new UserType { Id = 1, Name = "User " },
                new UserType { Id = 2, Name = "Employee" }
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
                    Price = 2200.00m,
                    CategoryId = 1,
                    ImageData = LoadImageData("wwwroot/images/prizes/highend wireless earbuds.jpg") // Load image data
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Jenga Boardgame",
                    Description = "Test your steady hand and strategic thinking with Jenga! Pull blocks from the tower and place them on top without making it tumble. A classic game of skill and suspense for all ages.",
                    Price = 1800.00m,
                    CategoryId = 4,
                    ImageData = LoadImageData("wwwroot/images/prizes/jenga boardgame.jpg") // Load image data
                },
                new Product
                {
                    ProductId = 3,
                    Name = "Plush Giant Bear",
                    Description = "Meet our incredibly soft and cuddly Plush Giant Bear! Perfect for big hugs and comforting snuggles, this lovable companion is ready to be your best friend. A timeless gift that brings joy to all ages.",
                    Price = 1500.00m,
                    CategoryId = 2,
                    ImageData = LoadImageData("wwwroot/images/prizes/plush giant bear.jpg") // Load image data
                },
                new Product
                {
                    ProductId = 4,
                    Name = "Multicable Charger",
                    Description = "Simplify your charging with our versatile Multicable Charger. Featuring multiple connectors, it's the perfect all-in-one solution to power up all your devices with just one cable.",
                    Price = 500.00m,
                    CategoryId = 1,
                    ImageData = LoadImageData("wwwroot/images/prizes/multicable charger.jpg") // Load image data
                },
                new Product
                {
                    ProductId = 5,
                    Name = "Stitch Keychain",
                    Description = "Carry a little bit of alien mischief with you everywhere! This adorable Stitch keychain features everyone's favorite mischievous blue alien, perfect for adding a touch of fun to your keys or bag.",
                    Price = 300.00m,
                    CategoryId = 2,
                    ImageData = LoadImageData("wwwroot/images/prizes/stitch keychain.jpeg") // Load image data
                },
                new Product
                {
                    ProductId = 6,
                    Name = "PS5 Console",
                    Description = "Dive into next-gen gaming with the PlayStation 5 console. Experience lightning-fast loading, immersive haptic feedback, adaptive triggers, and incredible 3D audio, bringing game worlds to life like never before.",
                    Price = 1000.00m,
                    CategoryId = 4,
                    ImageData = LoadImageData("wwwroot/images/prizes/PS5 console.jpg") // Load image data
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

            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    CustomerId = 1,
                    Name = "Admin",
                    Email = "admin@example.com",
                    Phone = "555-1234",
                    Address = "123 Admin St",
                    City = "AdminCity",
                    PostCode = "12345",
                    UserType = 0,
                    // Password: "AdminPass123!"
                    PasswordHash = "AQAAAAEAACcQAAAAEG5r2X4Jx9/7Rm4KjJkKZ2y5eFz7XlWz6JmRvg==",
                    Salt = "YWJjZGVmZ2hpamtsbW5vcA==",
                    AdminKey = "YourSecureAdminKey123",
                    AvatarImageData = defaultAvatarData,
                    PrizePoints = 1000,
                },
                new Customer
                {
                    CustomerId = 2,
                    Name = "Customer",
                    Email = "customer@example.com",
                    Phone = "555-5678",
                    Address = "456 Customer Ave",
                    City = "CustomerTown",
                    PostCode = "67890",
                    UserType = 1,
                    // Password: "CustomerPass456!"
                    PasswordHash = "AQAAAAEAACcQAAAAEDZR8vQlLk4Tn3A8jHKYpX9Tz1WY1qP9NlQ=",
                    Salt = "bW5vcGxxcnN0dXZ3eHk=",
                    AvatarImageData = defaultAvatarData,
                    PrizePoints = 100,
                }
            );
        }

        // Helper method to load image data from file
        private byte[] LoadImageData(string filePath)
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }
            return null; // Return null if the file does not exist
        }
    }
}
