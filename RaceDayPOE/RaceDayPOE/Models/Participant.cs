using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// A participant account. Participants browse events and enrol in categories.
    /// Kept in its own table so it is fully separate from Organisers.
    /// Maps to the Participants table in Part 1.sql.
    /// </summary>
    [Table("Participants")]
    public class Participant
    {
        /// <summary>Primary key (auto-generated identity).</summary>
        [Key]
        public int ParticipantID { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>Login email. Unique across the Participants table (index configured in the DbContext).</summary>
        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>BCrypt hash of the password. The plain-text password is never stored.</summary>
        [Required, MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Enrolments this participant has made (one participant makes many enrolments).</summary>
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}