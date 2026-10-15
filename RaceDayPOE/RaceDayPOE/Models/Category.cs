using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// A distance/age category within an event (e.g. "10km Individual").
    /// Participants pick one category when they enrol.
    /// Maps to the Categories table in Part 1.sql.
    /// </summary>
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        /// <summary>Foreign key to the event this category belongs to.</summary>
        public int EventID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Distance in kilometres, DECIMAL(6,2).</summary>
        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        /// <summary>Maximum number of enrolments allowed in this category.</summary>
        public int MaxParticipants { get; set; }

        /// <summary>Entry fee in rand, DECIMAL(10,2).</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; }

        public Event? Event { get; set; }

        /// <summary>Enrolments received by this category.</summary>
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}