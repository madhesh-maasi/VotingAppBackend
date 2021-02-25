using Microsoft.EntityFrameworkCore.Migrations;

namespace DAL.Migrations
{
    public partial class Language_options_Added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Options_Spanish",
                table: "MeetingQuestions");

            migrationBuilder.DropColumn(
                name: "Question_Spanish",
                table: "MeetingQuestions");

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "MeetingQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "MeetingQuestions");

            migrationBuilder.AddColumn<string>(
                name: "Options_Spanish",
                table: "MeetingQuestions",
                type: "nvarchar(75)",
                maxLength: 75,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Question_Spanish",
                table: "MeetingQuestions",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }
    }
}
