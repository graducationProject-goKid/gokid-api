using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_fk_tasks_subcategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_TaskTemplates_SubCategories_SubCategoryId1",
            //    table: "TaskTemplates");

            //migrationBuilder.DropIndex(
            //    name: "IX_TaskTemplates_SubCategoryId1",
            //    table: "TaskTemplates");

            //migrationBuilder.DropColumn(
            //    name: "SubCategoryId1",
            //    table: "TaskTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "SubCategoryId",
                table: "TaskTemplates",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            //migrationBuilder.CreateIndex(
            //    name: "IX_TaskTemplates_SubCategoryId",
            //    table: "TaskTemplates",
            //    column: "SubCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_SubCategories_SubCategoryId",
                table: "TaskTemplates",
                column: "SubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_SubCategories_SubCategoryId",
                table: "TaskTemplates");

            //migrationBuilder.DropIndex(
            //    name: "IX_TaskTemplates_SubCategoryId",
            //    table: "TaskTemplates");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubCategoryId",
                table: "TaskTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "SubCategoryId1",
            //    table: "TaskTemplates",
            //    type: "nvarchar(450)",
            //    nullable: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_TaskTemplates_SubCategoryId1",
            //    table: "TaskTemplates",
            //    column: "SubCategoryId1");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_TaskTemplates_SubCategories_SubCategoryId1",
            //    table: "TaskTemplates",
            //    column: "SubCategoryId1",
            //    principalTable: "SubCategories",
            //    principalColumn: "Id");
        }
    }
}
