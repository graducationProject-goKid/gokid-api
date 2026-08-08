using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_message_tochild_col : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageToChild",
                table: "Rewards",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageToChild",
                table: "Rewards");
        }
    }
}
