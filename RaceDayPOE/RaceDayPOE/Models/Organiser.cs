using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// An organiser account. Organisers create and manage events, venues, categories and results.
    /// Organisers are kept in their own table (separate from Participants) so one person
    /// can never hold both roles. Maps to the Organisers table in Part 1.sql.
    /// </summary>
    [Table("Organisers")]
    public class Organiser
    {
        /// <summary>Primary key (auto-generated identity).</summary>
        [Key]
        public int OrganiserID { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>Login email. Unique across the Organisers table (index configured in the DbContext).</summary>
        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>BCrypt hash of the password. The plain-text password is never stored.</summary>
        [Required, MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Events this organiser owns (one organiser organises many events).</summary>
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}