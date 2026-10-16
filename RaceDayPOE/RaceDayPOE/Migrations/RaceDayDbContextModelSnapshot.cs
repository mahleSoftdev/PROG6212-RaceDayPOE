using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RaceDayPOE.Data;
using RaceDayPOE.Models;

#nullable disable
namespace RaceDayPOE.Migrations;

[DbContext(typeof(RaceDayDbContext))]
partial class RaceDayDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.29");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity<Organiser>(b =>
        {
            b.ToTable("Organisers"); b.HasKey(x => x.OrganiserID);
            b.Property(x => x.OrganiserID).ValueGeneratedOnAdd(); b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(100).IsRequired(); b.Property(x => x.Email).HasMaxLength(255).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired(); b.Property(x => x.CreatedAt).HasColumnType("datetime");
            b.HasIndex(x => x.Email).IsUnique();
        });
        modelBuilder.Entity<Participant>(b =>
        {
            b.ToTable("Participants"); b.HasKey(x => x.ParticipantID);
            b.Property(x => x.ParticipantID).ValueGeneratedOnAdd(); b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(100).IsRequired(); b.Property(x => x.Email).HasMaxLength(255).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired(); b.Property(x => x.CreatedAt).HasColumnType("datetime");
            b.HasIndex(x => x.Email).IsUnique();
        });
        modelBuilder.Entity<Venue>(b =>
        {
            b.ToTable("Venues"); b.HasKey(x => x.VenueID); b.Property(x => x.VenueID).ValueGeneratedOnAdd();
            b.Property(x => x.Name).HasMaxLength(150).IsRequired(); b.Property(x => x.AddressLine).HasMaxLength(200).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired(); b.Property(x => x.Province).HasMaxLength(100).IsRequired();
            b.Property(x => x.Latitude).HasColumnType("decimal(9,6)"); b.Property(x => x.Longitude).HasColumnType("decimal(9,6)");
        });
        modelBuilder.Entity<Event>(b =>
        {
            b.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
            b.HasKey(x => x.EventID); b.Property(x => x.EventID).ValueGeneratedOnAdd(); b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500).IsRequired(); b.Property(x => x.EventDate).HasColumnType("date");
            b.Property(x => x.EventType).HasConversion<string>().HasMaxLength(20); b.Property(x => x.CreatedAt).HasColumnType("datetime");
            b.HasOne(x => x.Organiser).WithMany(x => x.Events).HasForeignKey(x => x.OrganiserID).HasConstraintName("FK_Events_Organisers").OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Venue).WithMany(x => x.Events).HasForeignKey(x => x.VenueID).HasConstraintName("FK_Events_Venues").OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("Categories"); b.HasKey(x => x.CategoryID); b.Property(x => x.CategoryID).ValueGeneratedOnAdd();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired(); b.Property(x => x.DistanceKm).HasColumnType("decimal(6,2)");
            b.Property(x => x.EntryFee).HasColumnType("decimal(10,2)");
            b.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventID).HasConstraintName("FK_Categories_Events").OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Enrolment>(b =>
        {
            b.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));
            b.HasKey(x => x.EnrolmentID); b.Property(x => x.EnrolmentID).ValueGeneratedOnAdd(); b.Property(x => x.EnrolmentDate).HasColumnType("datetime");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            b.HasOne(x => x.Participant).WithMany(x => x.Enrolments).HasForeignKey(x => x.ParticipantID).HasConstraintName("FK_Enrolments_Participants").OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Category).WithMany(x => x.Enrolments).HasForeignKey(x => x.CategoryID).HasConstraintName("FK_Enrolments_Categories").OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Result>(b =>
        {
            b.ToTable("Results"); b.HasKey(x => x.ResultID); b.Property(x => x.ResultID).ValueGeneratedOnAdd();
            b.Property(x => x.FinishTime).HasColumnType("time"); b.Property(x => x.RecordedAt).HasColumnType("datetime");
            b.HasOne(x => x.Enrolment).WithMany(x => x.Results).HasForeignKey(x => x.EnrolmentID).HasConstraintName("FK_Results_Enrolments").OnDelete(DeleteBehavior.Restrict);
        });
    }
}
