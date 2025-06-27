using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameCraft.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrizePoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailId);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "UserTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Name" },
                values: new object[,]
                {
                    { 1, "Electronics" },
                    { 2, "Toys" },
                    { 3, "Accessories" },
                    { 4, "Gaming" },
                    { 5, "Gift Cards" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "Address", "AvatarUrl", "City", "Email", "Name", "PasswordHash", "Phone", "PostCode", "PrizePoints", "Salt", "UserType" },
                values: new object[,]
                {
                    { 1, "123 Admin St", null, "AdminCity", "admin@example.com", "Admin User", "hashedpassword1", "555-1234", "12345", 1000, "salt1", 0 },
                    { 2, "456 Customer Ave", null, "CustomerTown", "customer@example.com", "Regular Customer", "hashedpassword2", "555-5678", "67890", 100, "salt2", 1 },
                    { 3, "789 Employee Rd", null, "EmployeeCity", "employee@example.com", "Employee User", "hashedpassword3", "555-8765", "54321", 500, "salt3", 2 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "CategoryId", "Description", "ImageUrl", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 1, "Experience premium sound and ultimate freedom with our High-End Wireless Earbuds. Enjoy crystal-clear audio, comfortable fit, and intuitive controls for an immersive listening experience on the go.", "/images/prizes/highend wireless earbuds.jpg", "High-End Wireless Earbuds", 2200.00m },
                    { 2, 4, "Test your steady hand and strategic thinking with Jenga! Pull blocks from the tower and place them on top without making it tumble. A classic game of skill and suspense for all ages.", "/images/prizes/jenga boardgame.jpg", "Jenga Boardgame", 1800.00m },
                    { 3, 2, "Meet our incredibly soft and cuddly Plush Giant Bear! Perfect for big hugs and comforting snuggles, this lovable companion is ready to be your best friend. A timeless gift that brings joy to all ages.", "/images/prizes/plush giant bear.jpg", "Plush Giant Bear", 1500.00m },
                    { 4, 1, "Simplify your charging with our versatile Multicable Charger. Featuring multiple connectors, it's the perfect all-in-one solution to power up all your devices with just one cable.", "/images/prizes/multicable charger.jpg", "Multicable Charger", 500.00m },
                    { 5, 2, "Carry a little bit of alien mischief with you everywhere! This adorable Stitch keychain features everyone's favorite mischievous blue alien, perfect for adding a touch of fun to your keys or bag.", "/images/prizes/stitch keychain.jpeg", "Stitch Keychain", 300.00m },
                    { 6, 4, "Dive into next-gen gaming with the PlayStation 5 console. Experience lightning-fast loading, immersive haptic feedback, adaptive triggers, and incredible 3D audio, bringing game worlds to life like never before.", "/images/prizes/PS5 console.jpg", "PS5 Console", 1000.00m }
                });

            migrationBuilder.InsertData(
                table: "UserTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "User " },
                    { 3, "Employee" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "UserTypes");
        }
    }
}
