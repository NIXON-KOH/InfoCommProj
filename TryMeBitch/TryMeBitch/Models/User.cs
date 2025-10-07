using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TryMeBitch.Models
{
    public class User : IdentityUser
    {

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid Key { get; set; } = Guid.NewGuid();

    }
}
