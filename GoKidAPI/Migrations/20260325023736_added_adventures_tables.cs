using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class added_adventures_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdventureTask_Adventure_AdventureId",
                table: "AdventureTask");

            migrationBuilder.DropForeignKey(
                name: "FK_AdventureTask_TaskTemplates_TaskTemplateId",
                table: "AdventureTask");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureProgress_Childrens_ChildId",
                table: "ChildAdventureProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureProgress_WeeklyAdventure_WeeklyAdventureId",
                table: "ChildAdventureProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTask_AdventureTask_AdventureTaskId",
                table: "ChildAdventureTask");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTask_ChildAdventureProgress_ChildAdventureProgressId",
                table: "ChildAdventureTask");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTask_Childrens_ChildId",
                table: "ChildAdventureTask");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTask_WeeklyAdventure_WeeklyAdventureId",
                table: "ChildAdventureTask");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyAdventure_Adventure_AdventureId",
                table: "WeeklyAdventure");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyAdventure_Classes_ClassId",
                table: "WeeklyAdventure");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeeklyAdventure",
                table: "WeeklyAdventure");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildAdventureTask",
                table: "ChildAdventureTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildAdventureProgress",
                table: "ChildAdventureProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdventureTask",
                table: "AdventureTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Adventure",
                table: "Adventure");

            migrationBuilder.RenameTable(
                name: "WeeklyAdventure",
                newName: "WeeklyAdventures");

            migrationBuilder.RenameTable(
                name: "ChildAdventureTask",
                newName: "ChildAdventureTasks");

            migrationBuilder.RenameTable(
                name: "ChildAdventureProgress",
                newName: "ChildAdventureProgresses");

            migrationBuilder.RenameTable(
                name: "AdventureTask",
                newName: "AdventureTasks");

            migrationBuilder.RenameTable(
                name: "Adventure",
                newName: "Adventures");

            migrationBuilder.RenameIndex(
                name: "IX_WeeklyAdventure_ClassId",
                table: "WeeklyAdventures",
                newName: "IX_WeeklyAdventures_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_WeeklyAdventure_AdventureId",
                table: "WeeklyAdventures",
                newName: "IX_WeeklyAdventures_AdventureId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTask_WeeklyAdventureId",
                table: "ChildAdventureTasks",
                newName: "IX_ChildAdventureTasks_WeeklyAdventureId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTask_ChildId",
                table: "ChildAdventureTasks",
                newName: "IX_ChildAdventureTasks_ChildId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTask_ChildAdventureProgressId",
                table: "ChildAdventureTasks",
                newName: "IX_ChildAdventureTasks_ChildAdventureProgressId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTask_AdventureTaskId",
                table: "ChildAdventureTasks",
                newName: "IX_ChildAdventureTasks_AdventureTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureProgress_WeeklyAdventureId",
                table: "ChildAdventureProgresses",
                newName: "IX_ChildAdventureProgresses_WeeklyAdventureId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureProgress_ChildId",
                table: "ChildAdventureProgresses",
                newName: "IX_ChildAdventureProgresses_ChildId");

            migrationBuilder.RenameIndex(
                name: "IX_AdventureTask_TaskTemplateId",
                table: "AdventureTasks",
                newName: "IX_AdventureTasks_TaskTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_AdventureTask_AdventureId",
                table: "AdventureTasks",
                newName: "IX_AdventureTasks_AdventureId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeeklyAdventures",
                table: "WeeklyAdventures",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildAdventureTasks",
                table: "ChildAdventureTasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildAdventureProgresses",
                table: "ChildAdventureProgresses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdventureTasks",
                table: "AdventureTasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Adventures",
                table: "Adventures",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureTasks_Adventures_AdventureId",
                table: "AdventureTasks",
                column: "AdventureId",
                principalTable: "Adventures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureTasks_TaskTemplates_TaskTemplateId",
                table: "AdventureTasks",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureProgresses_Childrens_ChildId",
                table: "ChildAdventureProgresses",
                column: "ChildId",
                principalTable: "Childrens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureProgresses_WeeklyAdventures_WeeklyAdventureId",
                table: "ChildAdventureProgresses",
                column: "WeeklyAdventureId",
                principalTable: "WeeklyAdventures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTasks_AdventureTasks_AdventureTaskId",
                table: "ChildAdventureTasks",
                column: "AdventureTaskId",
                principalTable: "AdventureTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTasks_ChildAdventureProgresses_ChildAdventureProgressId",
                table: "ChildAdventureTasks",
                column: "ChildAdventureProgressId",
                principalTable: "ChildAdventureProgresses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTasks_Childrens_ChildId",
                table: "ChildAdventureTasks",
                column: "ChildId",
                principalTable: "Childrens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTasks_WeeklyAdventures_WeeklyAdventureId",
                table: "ChildAdventureTasks",
                column: "WeeklyAdventureId",
                principalTable: "WeeklyAdventures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyAdventures_Adventures_AdventureId",
                table: "WeeklyAdventures",
                column: "AdventureId",
                principalTable: "Adventures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyAdventures_Classes_ClassId",
                table: "WeeklyAdventures",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdventureTasks_Adventures_AdventureId",
                table: "AdventureTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_AdventureTasks_TaskTemplates_TaskTemplateId",
                table: "AdventureTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureProgresses_Childrens_ChildId",
                table: "ChildAdventureProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureProgresses_WeeklyAdventures_WeeklyAdventureId",
                table: "ChildAdventureProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTasks_AdventureTasks_AdventureTaskId",
                table: "ChildAdventureTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTasks_ChildAdventureProgresses_ChildAdventureProgressId",
                table: "ChildAdventureTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTasks_Childrens_ChildId",
                table: "ChildAdventureTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ChildAdventureTasks_WeeklyAdventures_WeeklyAdventureId",
                table: "ChildAdventureTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyAdventures_Adventures_AdventureId",
                table: "WeeklyAdventures");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyAdventures_Classes_ClassId",
                table: "WeeklyAdventures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeeklyAdventures",
                table: "WeeklyAdventures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildAdventureTasks",
                table: "ChildAdventureTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildAdventureProgresses",
                table: "ChildAdventureProgresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdventureTasks",
                table: "AdventureTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Adventures",
                table: "Adventures");

            migrationBuilder.RenameTable(
                name: "WeeklyAdventures",
                newName: "WeeklyAdventure");

            migrationBuilder.RenameTable(
                name: "ChildAdventureTasks",
                newName: "ChildAdventureTask");

            migrationBuilder.RenameTable(
                name: "ChildAdventureProgresses",
                newName: "ChildAdventureProgress");

            migrationBuilder.RenameTable(
                name: "AdventureTasks",
                newName: "AdventureTask");

            migrationBuilder.RenameTable(
                name: "Adventures",
                newName: "Adventure");

            migrationBuilder.RenameIndex(
                name: "IX_WeeklyAdventures_ClassId",
                table: "WeeklyAdventure",
                newName: "IX_WeeklyAdventure_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_WeeklyAdventures_AdventureId",
                table: "WeeklyAdventure",
                newName: "IX_WeeklyAdventure_AdventureId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTasks_WeeklyAdventureId",
                table: "ChildAdventureTask",
                newName: "IX_ChildAdventureTask_WeeklyAdventureId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTasks_ChildId",
                table: "ChildAdventureTask",
                newName: "IX_ChildAdventureTask_ChildId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTasks_ChildAdventureProgressId",
                table: "ChildAdventureTask",
                newName: "IX_ChildAdventureTask_ChildAdventureProgressId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureTasks_AdventureTaskId",
                table: "ChildAdventureTask",
                newName: "IX_ChildAdventureTask_AdventureTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureProgresses_WeeklyAdventureId",
                table: "ChildAdventureProgress",
                newName: "IX_ChildAdventureProgress_WeeklyAdventureId");

            migrationBuilder.RenameIndex(
                name: "IX_ChildAdventureProgresses_ChildId",
                table: "ChildAdventureProgress",
                newName: "IX_ChildAdventureProgress_ChildId");

            migrationBuilder.RenameIndex(
                name: "IX_AdventureTasks_TaskTemplateId",
                table: "AdventureTask",
                newName: "IX_AdventureTask_TaskTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_AdventureTasks_AdventureId",
                table: "AdventureTask",
                newName: "IX_AdventureTask_AdventureId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeeklyAdventure",
                table: "WeeklyAdventure",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildAdventureTask",
                table: "ChildAdventureTask",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildAdventureProgress",
                table: "ChildAdventureProgress",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdventureTask",
                table: "AdventureTask",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Adventure",
                table: "Adventure",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureTask_Adventure_AdventureId",
                table: "AdventureTask",
                column: "AdventureId",
                principalTable: "Adventure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureTask_TaskTemplates_TaskTemplateId",
                table: "AdventureTask",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureProgress_Childrens_ChildId",
                table: "ChildAdventureProgress",
                column: "ChildId",
                principalTable: "Childrens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureProgress_WeeklyAdventure_WeeklyAdventureId",
                table: "ChildAdventureProgress",
                column: "WeeklyAdventureId",
                principalTable: "WeeklyAdventure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTask_AdventureTask_AdventureTaskId",
                table: "ChildAdventureTask",
                column: "AdventureTaskId",
                principalTable: "AdventureTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTask_ChildAdventureProgress_ChildAdventureProgressId",
                table: "ChildAdventureTask",
                column: "ChildAdventureProgressId",
                principalTable: "ChildAdventureProgress",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTask_Childrens_ChildId",
                table: "ChildAdventureTask",
                column: "ChildId",
                principalTable: "Childrens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChildAdventureTask_WeeklyAdventure_WeeklyAdventureId",
                table: "ChildAdventureTask",
                column: "WeeklyAdventureId",
                principalTable: "WeeklyAdventure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyAdventure_Adventure_AdventureId",
                table: "WeeklyAdventure",
                column: "AdventureId",
                principalTable: "Adventure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyAdventure_Classes_ClassId",
                table: "WeeklyAdventure",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
