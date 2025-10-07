namespace TryMeBitch.Models
{
    public class DigitalTwinStatus
    {
        public int Id { get; set; }
        public string DepotSlot { get; set; }
        public string TrainId { get; set; }
        public string Status { get; set; } // e.g. Maintenance, Idle, Dispatched
        public DateTime UpdatedAt { get; set; }
    }
}
