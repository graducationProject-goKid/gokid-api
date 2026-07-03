using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_levels_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LevelId",
                table: "Childrens",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    MinPoints = table.Column<int>(type: "int", nullable: false),
                    BadgeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BadgePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Childrens_LevelId",
                table: "Childrens",
                column: "LevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_Levels_LevelId",
                table: "Childrens",
                column: "LevelId",
                principalTable: "Levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_Levels_LevelId",
                table: "Childrens");

            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropIndex(
                name: "IX_Childrens_LevelId",
                table: "Childrens");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "Childrens");
        }
    }
}
