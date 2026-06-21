using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_classSupervisorRelationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_Class_ClassId",
                table: "Childrens");

            migrationBuilder.DropForeignKey(
                name: "FK_Class_Institutions_InstitutionId",
                table: "Class");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSupervisor_Class_ClassId",
                table: "ClassSupervisor");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSupervisor_Supervisors_SupervisorId",
                table: "ClassSupervisor");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyAdventure_Class_ClassId",
                table: "WeeklyAdventure");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSupervisor",
                table: "ClassSupervisor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Class",
                table: "Class");

            migrationBuilder.RenameTable(
                name: "ClassSupervisor",
                newName: "ClassSupervisors");

            migrationBuilder.RenameTable(
                name: "Class",
                newName: "Classes");

            migrationBuilder.RenameIndex(
                name: "IX_ClassSupervisor_SupervisorId",
                table: "ClassSupervisors",
                newName: "IX_ClassSupervisors_SupervisorId");

            migrationBuilder.RenameIndex(
                name: "IX_Class_InstitutionId",
                table: "Classes",
                newName: "IX_Classes_InstitutionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSupervisors",
                table: "ClassSupervisors",
                columns: new[] { "ClassId", "SupervisorId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Classes",
                table: "Classes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_Classes_ClassId",
                table: "Childrens",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Institutions_InstitutionId",
                table: "Classes",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSupervisors_Classes_ClassId",
                table: "ClassSupervisors",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSupervisors_Supervisors_SupervisorId",
                table: "ClassSupervisors",
                column: "SupervisorId",
                principalTable: "Supervisors",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_Classes_ClassId",
                table: "Childrens");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Institutions_InstitutionId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSupervisors_Classes_ClassId",
                table: "ClassSupervisors");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSupervisors_Supervisors_SupervisorId",
                table: "ClassSupervisors");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyAdventure_Classes_ClassId",
                table: "WeeklyAdventure");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSupervisors",
                table: "ClassSupervisors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Classes",
                table: "Classes");

            migrationBuilder.RenameTable(
                name: "ClassSupervisors",
                newName: "ClassSupervisor");

            migrationBuilder.RenameTable(
                name: "Classes",
                newName: "Class");

            migrationBuilder.RenameIndex(
                name: "IX_ClassSupervisors_SupervisorId",
                table: "ClassSupervisor",
                newName: "IX_ClassSupervisor_SupervisorId");

            migrationBuilder.RenameIndex(
                name: "IX_Classes_InstitutionId",
                table: "Class",
                newName: "IX_Class_InstitutionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSupervisor",
                table: "ClassSupervisor",
                columns: new[] { "ClassId", "SupervisorId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Class",
                table: "Class",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_Class_ClassId",
                table: "Childrens",
                column: "ClassId",
                principalTable: "Class",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Class_Institutions_InstitutionId",
                table: "Class",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSupervisor_Class_ClassId",
                table: "ClassSupervisor",
                column: "ClassId",
                principalTable: "Class",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSupervisor_Supervisors_SupervisorId",
                table: "ClassSupervisor",
                column: "SupervisorId",
                principalTable: "Supervisors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyAdventure_Class_ClassId",
                table: "WeeklyAdventure",
                column: "ClassId",
                principalTable: "Class",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
