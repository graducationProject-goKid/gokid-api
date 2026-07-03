using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class fix_Institution_InstitutionAdmin_AspNetUser_relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstitutionAdmins_AspNetUsers_AppUserId",
                table: "InstitutionAdmins");

            migrationBuilder.DropIndex(
                name: "IX_InstitutionAdmins_AppUserId",
                table: "InstitutionAdmins");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "InstitutionAdmins");

            migrationBuilder.AddForeignKey(
                name: "FK_InstitutionAdmins_AspNetUsers_Id",
                table: "InstitutionAdmins",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstitutionAdmins_AspNetUsers_Id",
                table: "InstitutionAdmins");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "InstitutionAdmins",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionAdmins_AppUserId",
                table: "InstitutionAdmins",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InstitutionAdmins_AspNetUsers_AppUserId",
                table: "InstitutionAdmins",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
