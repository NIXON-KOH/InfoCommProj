namespace TryMeBitch.Models
{
    public class Station
    {
        public Guid StationID { get; set; }
        public string StationName { get; set; }
        public string Type { get; set; }
        public string StationLine {  get; set; }
        public decimal Lat { get; set; }
        public decimal Lon { get; set; }
        public double Distance { get; set; }
        public Guid NextStation { get; set; }
        public bool Active { get; set; }
        
    }
}
