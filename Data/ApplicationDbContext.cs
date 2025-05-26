using GameCraft.Models;
using Microsoft.EntityFrameworkCore;

namespace GameCraft.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSets here, e.g.:
        public DbSet<Customer> Customer { get; set; }
    }
}
