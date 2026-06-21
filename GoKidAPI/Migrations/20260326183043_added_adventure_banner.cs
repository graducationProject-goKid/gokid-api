using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class added_adventure_banner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InstitutionId",
                table: "Adventures",
                type: "nvarchar(150)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BannerImagePublicId",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionVoicePublicId",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_InstitutionId",
                table: "Adventures",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Adventures_Institutions_InstitutionId",
                table: "Adventures",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adventures_Institutions_InstitutionId",
                table: "Adventures");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_InstitutionId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "BannerImagePublicId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "DescriptionVoicePublicId",
                table: "Adventures");

            migrationBuilder.AlterColumn<string>(
                name: "InstitutionId",
                table: "Adventures",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
