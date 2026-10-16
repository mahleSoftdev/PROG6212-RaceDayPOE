using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    //Links a participant to a category (and therefore to an event).
    //Maps to the Enrolments table in Part 1.sql.
    [Table("Enrolments")]
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        //Foreign key to the participant who enrolled.
        public int ParticipantID { get; set; }

        //Foreign key to the category the participant selected.
        public int CategoryID { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        //Pending, Confirmed or Cancelled. Stored as text, NVARCHAR(50).
        public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Pending;

        public Participant? Participant { get; set; }
        public Category? Category { get; set; }

        //The recorded result(s) for this enrolment (the API allows only one).
        public ICollection<Result> Results { get; set; } = new List<Result>();
    }
}