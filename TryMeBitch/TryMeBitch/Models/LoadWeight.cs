namespace TryMeBitch.Models
{
    public class LoadWeight
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public double Kilograms { get; set; }
        public string TrainId { get; set; } = "";
    }
}
