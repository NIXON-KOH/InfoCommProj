using System;
using System.ComponentModel.DataAnnotations;

namespace TryMeBitch.Models
{
    public class TrainLocation
    {
        public int Id { get; set; }
        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public string TrainId { get; set; } = "";

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // ← add this
        public Train Train { get; set; } = null!;
    }
}
