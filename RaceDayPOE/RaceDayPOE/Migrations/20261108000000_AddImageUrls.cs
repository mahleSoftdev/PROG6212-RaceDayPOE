using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDayPOE.Data;

#nullable disable
namespace RaceDayPOE.Migrations;

//Part 3: stores the Azure Blob Storage URLs for event banners and participant profile pictures.
[DbContext(typeof(RaceDayDbContext))]
[Migration("20261108000000_AddImageUrls")]
public partial class AddImageUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "BannerUrl", table: "Events", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProfilePictureUrl", table: "Participants", type: "nvarchar(500)", maxLength: 500, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BannerUrl", table: "Events");
        migrationBuilder.DropColumn(name: "ProfilePictureUrl", table: "Participants");
    }
}
