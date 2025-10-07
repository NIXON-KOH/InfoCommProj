using System.ComponentModel.DataAnnotations.Schema;

namespace TryMeBitch.Models
{
    public class AlertHistory
    {
        public int Id { get; set; }
        public int DefinitionId { get; set; }

        [ForeignKey(nameof(DefinitionId))]
        public AlertDefinition Definition { get; set; } = null!;

        public DateTime FiredAt { get; set; }
        public string RecipientEmail { get; set; } = "";
        public double ObservedValue { get; set; }
        public string? MessageSent { get; set; }
    }
}
