using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DevTrack.Infrastructure.Data;

#nullable disable

namespace DevTrack.Infrastructure.Migrations;

[DbContext(typeof(DevTrackDbContext))]
[Migration("20261024110000_AddProjectOwnership")]
public partial class AddProjectOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OwnerId",
            table: "Projects",
            type: "nvarchar(450)",
            maxLength: 450,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_Projects_OwnerId",
            table: "Projects",
            column: "OwnerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Projects_OwnerId", table: "Projects");
        migrationBuilder.DropColumn(name: "OwnerId", table: "Projects");
    }
}
