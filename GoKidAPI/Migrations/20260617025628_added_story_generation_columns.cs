using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class added_story_generation_columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoryTitle",
                table: "AdventureTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntroStory",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntroTitle",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntroVoicePublicId",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntroVoiceUrl",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutroStory",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutroTitle",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutroVoicePublicId",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutroVoiceUrl",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoryTitle",
                table: "AdventureTasks");

            migrationBuilder.DropColumn(
                name: "IntroStory",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "IntroTitle",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "IntroVoicePublicId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "IntroVoiceUrl",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "OutroStory",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "OutroTitle",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "OutroVoicePublicId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "OutroVoiceUrl",
                table: "Adventures");
        }
    }
}
