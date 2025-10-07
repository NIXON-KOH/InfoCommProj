namespace TryMeBitch.Models
{
    public class BrakePressureLog
    {
        public int Id { get; set; }
        public string TrainId { get; set; }
        public float Pressure { get; set; }
        public string Status { get; set; } // e.g. Normal, Warning, Critical
        public DateTime Timestamp { get; set; }
    }
}
