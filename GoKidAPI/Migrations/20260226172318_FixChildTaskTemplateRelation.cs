using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixChildTaskTemplateRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropForeignKey(
      name: "FK_ChildTasks_TaskTemplates_TaskTemplateId",
      table: "ChildTasks");

            migrationBuilder.AlterColumn<string>(
                name: "TaskTemplateId",
                table: "ChildTasks",
                type: "nvarchar(150)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

         
            migrationBuilder.AddForeignKey(
                name: "FK_ChildTasks_TaskTemplates_TaskTemplateId",
                table: "ChildTasks",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChildTasks_TaskTemplates_TaskTemplateId",
                table: "ChildTasks");

  
            migrationBuilder.AlterColumn<string>(
                name: "TaskTemplateId",
                table: "ChildTasks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");


           
        }
    }
}
