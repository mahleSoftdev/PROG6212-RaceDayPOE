using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDayPOE.Data;

#nullable disable
namespace RaceDayPOE.Migrations;

[DbContext(typeof(RaceDayDbContext))]
[Migration("20261015000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Organisers", table => new
        {
            OrganiserID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            PasswordHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Organisers", x => x.OrganiserID));

        migrationBuilder.CreateTable("Participants", table => new
        {
            ParticipantID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            PasswordHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Participants", x => x.ParticipantID));

        migrationBuilder.CreateTable("Venues", table => new
        {
            VenueID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
            AddressLine = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
            Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Venues", x => x.VenueID));

        migrationBuilder.CreateTable("Events", table => new
        {
            EventID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            OrganiserID = table.Column<int>(type: "int", nullable: false),
            VenueID = table.Column<int>(type: "int", nullable: false),
            Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
            Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
            EventDate = table.Column<DateTime>(type: "date", nullable: false),
            EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Events", x => x.EventID);
            table.CheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
            table.ForeignKey("FK_Events_Organisers", x => x.OrganiserID, "Organisers", "OrganiserID", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_Events_Venues", x => x.VenueID, "Venues", "VenueID", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("Categories", table => new
        {
            CategoryID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            EventID = table.Column<int>(type: "int", nullable: false),
            Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
            MaxParticipants = table.Column<int>(type: "int", nullable: false),
            EntryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Categories", x => x.CategoryID);
            table.ForeignKey("FK_Categories_Events", x => x.EventID, "Events", "EventID", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("Enrolments", table => new
        {
            EnrolmentID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ParticipantID = table.Column<int>(type: "int", nullable: false),
            CategoryID = table.Column<int>(type: "int", nullable: false),
            EnrolmentDate = table.Column<DateTime>(type: "datetime", nullable: false),
            Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Enrolments", x => x.EnrolmentID);
            table.CheckConstraint("CK_Enrolments_Status", "[Status] IN ('Pending', 'Confirmed', 'Cancelled')");
            table.ForeignKey("FK_Enrolments_Categories", x => x.CategoryID, "Categories", "CategoryID", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_Enrolments_Participants", x => x.ParticipantID, "Participants", "ParticipantID", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("Results", table => new
        {
            ResultID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            EnrolmentID = table.Column<int>(type: "int", nullable: false),
            FinishTime = table.Column<TimeSpan>(type: "time", nullable: false),
            Position = table.Column<int>(type: "int", nullable: false),
            RecordedAt = table.Column<DateTime>(type: "datetime", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Results", x => x.ResultID);
            table.ForeignKey("FK_Results_Enrolments", x => x.EnrolmentID, "Enrolments", "EnrolmentID", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateIndex("IX_Organisers_Email", "Organisers", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Participants_Email", "Participants", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Events_OrganiserID", "Events", "OrganiserID");
        migrationBuilder.CreateIndex("IX_Events_VenueID", "Events", "VenueID");
        migrationBuilder.CreateIndex("IX_Categories_EventID", "Categories", "EventID");
        migrationBuilder.CreateIndex("IX_Enrolments_CategoryID", "Enrolments", "CategoryID");
        migrationBuilder.CreateIndex("IX_Enrolments_ParticipantID", "Enrolments", "ParticipantID");
        migrationBuilder.CreateIndex("IX_Results_EnrolmentID", "Results", "EnrolmentID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Results");
        migrationBuilder.DropTable("Enrolments");
        migrationBuilder.DropTable("Categories");
        migrationBuilder.DropTable("Events");
        migrationBuilder.DropTable("Participants");
        migrationBuilder.DropTable("Organisers");
        migrationBuilder.DropTable("Venues");
    }
}
