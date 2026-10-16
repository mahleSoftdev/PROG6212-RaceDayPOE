using Microsoft.EntityFrameworkCore;
using RaceDayPOE.Models;

namespace RaceDayPOE.Data
{
   
    //The EF Core database context. Each DbSet is one table in racedayDB, and
    // OnModelCreating adds the rules (unique emails, enum-to-text, CHECK constraints,
    // foreign keys) so the generated schema matches Part 1.sql and the ERD.
    
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<Organiser> Organisers => Set<Organiser>();
        public DbSet<Participant> Participants => Set<Participant>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Emails must be unique within each user table (Email ... UNIQUE in the SQL).
            modelBuilder.Entity<Organiser>().HasIndex(o => o.Email).IsUnique();
            modelBuilder.Entity<Participant>().HasIndex(p => p.Email).IsUnique();

            // Events: enum stored as text, limited to Run/Walk/Cycle, plus its two foreign keys.
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.EventType)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));

                entity.HasOne(e => e.Organiser)
                      .WithMany(o => o.Events)
                      .HasForeignKey(e => e.OrganiserID)
                      .HasConstraintName("FK_Events_Organisers")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Venue)
                      .WithMany(v => v.Events)
                      .HasForeignKey(e => e.VenueID)
                      .HasConstraintName("FK_Events_Venues")
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Categories: each belongs to one event.
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID)
                .HasConstraintName("FK_Categories_Events")
                .OnDelete(DeleteBehavior.Restrict);

            // Enrolments: status stored as text, plus links to Participant and Category.
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.Property(e => e.Status)
                      .HasConversion<string>()
                      .HasMaxLength(50);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Enrolments_Status", "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));

                entity.HasOne(e => e.Participant)
                      .WithMany(p => p.Enrolments)
                      .HasForeignKey(e => e.ParticipantID)
                      .HasConstraintName("FK_Enrolments_Participants")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(e => e.CategoryID)
                      .HasConstraintName("FK_Enrolments_Categories")
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Results: each belongs to one enrolment.
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithMany(e => e.Results)
                .HasForeignKey(r => r.EnrolmentID)
                .HasConstraintName("FK_Results_Enrolments")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}