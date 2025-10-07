namespace TryMeBitch.Models
{
    public class RFIDEntryLog
    {
        public int Id { get; set; }
        public string TrainId { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public string EntryStatus { get; set; }
    }

}
