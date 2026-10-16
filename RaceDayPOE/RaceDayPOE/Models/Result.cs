using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    // The finishing time and position recorded for an enrolment after the event.
    // Maps to the Results table in Part 1.sql.
    [Table("Results")]
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        //Foreign key to the enrolment this result belongs to.
        public int EnrolmentID { get; set; }

        //Finish time as a duration (hh:mm:ss). Stored as SQL TIME.
        [Column(TypeName = "time")]
        public TimeSpan FinishTime { get; set; }

        //Finishing position (1 = first).
        public int Position { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public Enrolment? Enrolment { get; set; }
    }
}