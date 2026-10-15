using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// The finishing time and position recorded for an enrolment after the event.
    /// Maps to the Results table in Part 1.sql.
    /// </summary>
    [Table("Results")]
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        /// <summary>Foreign key to the enrolment this result belongs to.</summary>
        public int EnrolmentID { get; set; }

        /// <summary>Finish time as a duration (hh:mm:ss). Stored as SQL TIME.</summary>
        [Column(TypeName = "time")]
        public TimeSpan FinishTime { get; set; }

        /// <summary>Finishing position (1 = first).</summary>
        public int Position { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public Enrolment? Enrolment { get; set; }
    }
}