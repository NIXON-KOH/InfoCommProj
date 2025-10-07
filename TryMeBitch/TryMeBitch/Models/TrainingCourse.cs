using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TryMeBitch.Models
{
    public class TrainingCourse
    {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int InputId { get; set; }
            public string Email { get; set; }
            public string CourseName { get; set; }
    }
}
