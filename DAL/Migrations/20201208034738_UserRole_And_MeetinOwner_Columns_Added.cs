using Microsoft.EntityFrameworkCore.Migrations;

namespace DAL.Migrations
{
    public partial class UserRole_And_MeetinOwner_Columns_Added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingOwner",
                table: "Meetings",
                type: "varchar(75)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingOwnerEmail",
                table: "Meetings",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MeetingOwnerId",
                table: "Meetings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MeetingOwner",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "MeetingOwnerEmail",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "MeetingOwnerId",
                table: "Meetings");
        }
    }
}
