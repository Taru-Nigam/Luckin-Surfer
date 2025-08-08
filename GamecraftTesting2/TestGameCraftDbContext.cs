using Microsoft.EntityFrameworkCore;
using GameCraft.Data;
using GameCraft.Models;

public class TestGameCraftDbContext : GameCraftDbContext
{
    public TestGameCraftDbContext(DbContextOptions<GameCraftDbContext> options)
        : base(options)
    {
    }

    // Avoid loading the actual database file when creating the model
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Only establish the primary key
        modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
    }
}
