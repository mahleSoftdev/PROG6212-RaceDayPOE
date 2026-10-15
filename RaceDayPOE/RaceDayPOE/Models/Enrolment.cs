using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// Links a participant to a category (and therefore to an event).
    /// Maps to the Enrolments table in Part 1.sql.
    /// </summary>
    [Table("Enrolments")]
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        /// <summary>Foreign key to the participant who enrolled.</summary>
        public int ParticipantID { get; set; }

        /// <summary>Foreign key to the category the participant selected.</summary>
        public int CategoryID { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        /// <summary>Pending, Confirmed or Cancelled. Stored as text, NVARCHAR(50).</summary>
        public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Pending;

        public Participant? Participant { get; set; }
        public Category? Category { get; set; }

        /// <summary>The recorded result(s) for this enrolment (the API allows only one).</summary>
        public ICollection<Result> Results { get; set; } = new List<Result>();
    }
}