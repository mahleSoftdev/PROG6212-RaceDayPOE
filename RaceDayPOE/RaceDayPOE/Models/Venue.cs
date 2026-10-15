using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
    /// <summary>
    /// A physical location where events are hosted. This is the "location" of an event.
    /// Maps to the Venues table in Part 1.sql.
    /// </summary>
    [Table("Venues")]
    public class Venue
    {
        [Key]
        public int VenueID { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string AddressLine { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        /// <summary>Latitude in decimal degrees, DECIMAL(9,6).</summary>
        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }

        /// <summary>Longitude in decimal degrees, DECIMAL(9,6).</summary>
        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        /// <summary>Events hosted at this venue (one venue hosts many events).</summary>
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}