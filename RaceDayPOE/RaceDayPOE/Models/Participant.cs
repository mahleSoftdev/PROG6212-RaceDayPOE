using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
   
    //A participant account. Participants browse events and enrol in categories.
    //Kept in its own table so it is fully separate from Organisers.
    //Maps to the Participants table in Part 1.sql.
    
    [Table("Participants")]
    public class Participant
    {
        //Primary key (auto-generated identity).
        [Key]
        public int ParticipantID { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        //Login email. Unique across the Participants table (index configured in the DbContext).
        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        //ASP.NET Core Identity password hash (PBKDF2). The plain-text password is never stored.
        [Required, MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        //Public URL of the profile picture stored in Azure Blob Storage (null until one is uploaded).
        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Enrolments this participant has made (one participant makes many enrolments).
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}