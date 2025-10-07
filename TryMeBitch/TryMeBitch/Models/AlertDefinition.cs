using System.ComponentModel.DataAnnotations;

namespace TryMeBitch.Models
{

    public enum AlertType
    {
        BayPower = 0,
        CapacityUtilization = 1,
        WheelMaintenance = 2   // 👈 NEW
    }
    public class AlertDefinition
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        public AlertType Type { get; set; }

        [Required]
        public string TargetId { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";

        [Required]
        [Range(0.1, 100.0, ErrorMessage = "Threshold must be between 0.1% and 100%.")]
        public double Threshold { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Message template is too long (max 200 chars).")]
        public string MessageTemplate { get; set; } = "";

        public DateTime? LastFiredAt { get; set; }
    }
}
