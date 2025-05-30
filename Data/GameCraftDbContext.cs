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
        public DbSet<UserType> RoleId { get; set; }


    }
}
