using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_fk_subcategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                    name: "FK_TaskTemplates_SubCategories_TaskSubCategoryId",
                    table: "TaskTemplates");

            migrationBuilder.RenameColumn(
                name: "TaskSubCategoryId",
                table: "TaskTemplates",
                newName: "SubCategoryId"); // استخدم اسم مطابق للـ property

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplates_TaskSubCategoryId",
                table: "TaskTemplates",
                newName: "IX_TaskTemplates_SubCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_SubCategories_SubCategoryId",
                table: "TaskTemplates",
                column: "SubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the FK added in Up
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_SubCategories_SubCategoryId",
                table: "TaskTemplates");

            // Rename the column back to original
            migrationBuilder.RenameColumn(
                name: "SubCategoryId",
                table: "TaskTemplates",
                newName: "TaskSubCategoryId");

            // Rename the index back to original
            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplates_SubCategoryId",
                table: "TaskTemplates",
                newName: "IX_TaskTemplates_TaskSubCategoryId");

            // Add the old FK back
            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_SubCategories_TaskSubCategoryId",
                table: "TaskTemplates",
                column: "TaskSubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id");
        }

    }
}
