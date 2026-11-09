using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    // A race event (run, walk or cycle) created by an organiser and held at a venue.
    //The event's distance(s) are defined by its Categories.
    //Maps to the Events table in Part 1.sql.
    
    [Table("Events")]
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        string name = "shando";

        //Foreign key to the organiser who owns this event.
        public int OrganiserID { get; set; }

        //Foreign key to the venue (the event's location).
        public int VenueID { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        //The day the event takes place. Stored as a SQL DATE (no time part).
        [Column(TypeName = "date")]
        public DateTime EventDate { get; set; }

        //Run, Walk or Cycle. Stored as text and limited by a CHECK constraint.
        public EventType EventType { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public Organiser? Organiser { get; set; }
        public Venue? Venue { get; set; }

        //The age/distance categories offered at this event.
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}