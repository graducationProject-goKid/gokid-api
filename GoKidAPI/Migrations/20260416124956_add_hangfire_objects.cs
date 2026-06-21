using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_hangfire_objects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Adventures",
                newName: "TitleEn");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Adventures",
                newName: "TitleAr");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoalAr",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoalEn",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "GoalAr",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "GoalEn",
                table: "Adventures");

            migrationBuilder.RenameColumn(
                name: "TitleEn",
                table: "Adventures",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "Adventures",
                newName: "Description");
        }
    }
}
