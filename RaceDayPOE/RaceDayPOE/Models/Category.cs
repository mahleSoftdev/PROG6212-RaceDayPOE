using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    
    //A distance/age category within an event (e.g. "10km Individual").
    //Participants pick one category when they enrol.
    //Maps to the Categories table in Part 1.sql.
    
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        //Foreign key to the event this category belongs to.
        public int EventID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        //Distance in kilometres, DECIMAL(6,2).
        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        //Maximum number of enrolments allowed in this category.
        public int MaxParticipants { get; set; }

        //Entry fee in rand, DECIMAL(10,2).
        [Column(TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; }

        public Event? Event { get; set; }

        //Enrolments received by this category.
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}