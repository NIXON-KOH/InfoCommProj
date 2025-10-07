using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;

public class MRTDbContext : IdentityDbContext<User>
{
    public MRTDbContext(DbContextOptions<MRTDbContext> options) : base(options) {}
    public DbSet<Station> stations {  get; set; }
    public DbSet<Blockchain.BlockChainEvent> Blockchain { get; set; }
    public DbSet<HVAC> HVAC { get; set; }

    public DbSet<Issues> Issues { get; set; }
    public DbSet<comment> comments { get; set; }   
    public DbSet<Timeline> Timelines { get; set; }
    public DbSet<Threshold> Threshold { get; set; }
    public DbSet<User> Users { get; set; }

    public DbSet<WheelScan> WheelScans { get; set; }
    public DbSet<TrainLocation> TrainLocations { get; set; }
    public DbSet<PowerUsage> PowerUsages { get; set; }
    public DbSet<LoadWeight> LoadWeights { get; set; }
    public DbSet<DepotEnergySlot> DepotEnergySlots { get; set; }
    public DbSet<AlertDefinition> AlertDefinitions { get; set; }

    public DbSet<Train> Train { get; set; }

    public DbSet<AlertHistory> AlertHistories { get; set; }
    public DbSet<BrakePressureLog> Joel_BrakePressureLogs { get; set; }
    public DbSet<CabinTempLog> Joel_CabinTempLogs { get; set; }
    public DbSet<RFIDEntryLog> Joel_RFIDEntryLogs { get; set; }
    public DbSet<Joel_Train> Joel_Train { get; set; }

    public DbSet<TrainingCourse> TrainingCourses { get; set; }

    public DbSet<DigitalTwinStatus> DigitalTwinStatuses { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); 
    }

}

