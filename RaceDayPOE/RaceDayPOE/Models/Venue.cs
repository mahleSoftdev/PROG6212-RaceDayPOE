using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayPOE.Models
{
   
    //A physical location where events are hosted. This is the "location" of an event.
    //Maps to the Venues table in Part 1.sql.
 
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

        //Latitude in decimal degrees, DECIMAL(9,6)
        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }

        //Longitude in decimal degrees, DECIMAL(9,6)].
        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        //Events hosted at this venue (one venue hosts many events).
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}