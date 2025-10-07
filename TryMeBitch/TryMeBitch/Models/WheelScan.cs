namespace TryMeBitch.Models
{
    public class WheelScan
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string TrainId { get; set; } = null!;
        public int WheelPosition { get; set; } // e.g. 1–8 for an 8-wheel bogie
        public double Distance { get; set; } // mm from sensor

        // whether this reading deviates > threshold from baseline
        public bool IsAnomaly { get; set; }
    }
}

