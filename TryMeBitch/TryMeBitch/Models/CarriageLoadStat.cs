namespace TryMeBitch.Models
{
    public class CarriageLoadStat
    {
        public string CarriageId { get; set; } = "";
        public double AvgLoad { get; set; }
        public double MaxLoad { get; set; }
        public double CurrentLoad { get; set; }  // new
        public double Capacity { get; set; } = 3000; // example
        public double UtilizationPct => CurrentLoad / Capacity * 100;
    }

}
