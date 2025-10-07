    namespace TryMeBitch.Models
{
    public class CabinTempLog
    {
        public int Id { get; set; }
        public string TrainId { get; set; }
        public float Temperature { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
