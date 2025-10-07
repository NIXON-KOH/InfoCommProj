namespace TryMeBitch.Models
{
    public class PowerUsage
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }    // e.g. "Train" or "Bay-01"
        public double Watts { get; set; }
    }
}
