using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_task_template_recommended_age_cols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecommendedAgeFrom",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecommendedAgeTo",
                table: "TaskTemplates",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecommendedAgeFrom",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "RecommendedAgeTo",
                table: "TaskTemplates");
        }
    }
}
