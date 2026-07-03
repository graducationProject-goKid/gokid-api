using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_instiution_data_col : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoPublicId",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "LogoPublicId",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Institutions");
        }
    }
}
