using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TryMeBitch.Models
{
    [Table("Joel_Train")]

    public class Joel_Train
    {

        [Key]
        public int Id { get; set; }

        [Display(Name = "Train ID")]
        public string TrainId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Display(Name = "# Carriages")]
        [Range(1, 20)]
        public int NumCarriages { get; set; }

        [Display(Name = "Active?")]
        public bool IsActive { get; set; }

        public string Line { get; set; }      // From your original SQL

        public string Status { get; set; }    // From your original SQL
    }
}
