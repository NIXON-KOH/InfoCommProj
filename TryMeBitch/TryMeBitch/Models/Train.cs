namespace TryMeBitch.Models
{
    public class Train
    {
        public string Id { get; set; } = "";   // e.g. "Train1"
        public string Name { get; set; } = ""; // optional descriptive name
        public ICollection<TrainLocation> Locations { get; set; } = new List<TrainLocation>();
    }
}
