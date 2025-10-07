namespace TryMeBitch.Models
{
    public class DepotEnergySlot
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string BayId { get; set; }
        public double Watts { get; set; }
    }
}
