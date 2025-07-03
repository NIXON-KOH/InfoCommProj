using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;
namespace TryMeBitch.Data
{
    public class MRTDbContext : DbContext
    {
        public MRTDbContext(DbContextOptions options) : base(options) {}
        public DbSet<Station> stations {  get; set; }
        public DbSet<Blockchain.Card> cards { get; set; }
        public DbSet<Blockchain.BlockchainEvent> Blockchain { get; set; }
        public DbSet<HVAC> HVAC { get; set; }

        public DbSet<Threshold> Threshold { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 
        }

    }
}
