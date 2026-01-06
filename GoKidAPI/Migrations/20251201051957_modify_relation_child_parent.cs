using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class modify_relation_child_parent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_Parents_ParentId",
                table: "Childrens");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_AspNetUsers_ParentId",
                table: "Childrens",
                column: "ParentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_AspNetUsers_ParentId",
                table: "Childrens");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_Parents_ParentId",
                table: "Childrens",
                column: "ParentId",
                principalTable: "Parents",
                principalColumn: "Id");
        }
    }
}
