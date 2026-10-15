using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// A race event (run, walk or cycle) created by an organiser and held at a venue.
    /// The event's distance(s) are defined by its Categories.
    /// Maps to the Events table in Part 1.sql.
    /// </summary>
    [Table("Events")]
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        /// <summary>Foreign key to the organiser who owns this event.</summary>
        public int OrganiserID { get; set; }

        /// <summary>Foreign key to the venue (the event's location).</summary>
        public int VenueID { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>The day the event takes place. Stored as a SQL DATE (no time part).</summary>
        [Column(TypeName = "date")]
        public DateTime EventDate { get; set; }

        /// <summary>Run, Walk or Cycle. Stored as text and limited by a CHECK constraint.</summary>
        public EventType EventType { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Organiser? Organiser { get; set; }
        public Venue? Venue { get; set; }

        /// <summary>The age/distance categories offered at this event.</summary>
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}