namespace TryMeBitch.Models
{
    public class TelemetryViewModel
    {
        public List<TrainLocation> TrainLocations { get; set; }
        public List<PowerUsage> PowerUsages { get; set; }
        public List<LoadWeight> LoadWeights { get; set; }
        public List<DepotEnergySlot> DepotEnergySlots { get; set; }
    }
}
