namespace TryMeBitch.Models
{
    public class BayEnergyStat
    {
        public string BayId { get; set; } = "";
        public double AvgWatts { get; set; }
        public double MinWatts { get; set; }
        public double MaxWatts { get; set; }
    }
 
    public class EnergyMonitoringViewModel
    {
        public List<BayEnergyStat> Stats { get; set; } = new();
        public List<DepotEnergySlot> Alerts { get; set; } = new();
        public List<TimeSeriesPoint> GroupedReadings { get; set; } = new();
        public double ThresholdValue { get; set; }

        public double CurrentUsageOverall { get; set; }
        public double PeakOverall { get; set; }
        public double AvgOverall { get; set; }
    }
}
