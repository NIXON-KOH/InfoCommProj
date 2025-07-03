namespace TryMeBitch.Models
{
    public class Threshold
    {
        public Guid Id { get; set; }
        public double temperature { get; set; }
        public double humidity { get; set; }
        public double psi { get; set; }
        public double GasDetection { get; set; }
        public override string ToString()
        {
            return $"Id: {Id}, Temp: {temperature}, Humidity: {humidity}, PSI: {psi}, GasDetection: {GasDetection}";
        }
    }
}
