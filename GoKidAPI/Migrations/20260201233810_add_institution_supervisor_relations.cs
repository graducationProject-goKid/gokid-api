using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_institution_supervisor_relations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_Institution_InstitutionId",
                table: "Childrens");

            migrationBuilder.DropForeignKey(
                name: "FK_Class_Institution_InstitutionId",
                table: "Class");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSupervisor_Supervisor_SupervisorId",
                table: "ClassSupervisor");

            migrationBuilder.DropForeignKey(
                name: "FK_Institution_InstitutionAdmin_InstitutionAdminId",
                table: "Institution");

            migrationBuilder.DropForeignKey(
                name: "FK_InstitutionAdmin_AspNetUsers_AppUserId",
                table: "InstitutionAdmin");

            migrationBuilder.DropForeignKey(
                name: "FK_Supervisor_AspNetUsers_AppUserId",
                table: "Supervisor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Supervisor",
                table: "Supervisor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InstitutionAdmin",
                table: "InstitutionAdmin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Institution",
                table: "Institution");

            migrationBuilder.DropIndex(
                name: "IX_Institution_InstitutionAdminId",
                table: "Institution");

            migrationBuilder.RenameTable(
                name: "Supervisor",
                newName: "Supervisors");

            migrationBuilder.RenameTable(
                name: "InstitutionAdmin",
                newName: "InstitutionAdmins");

            migrationBuilder.RenameTable(
                name: "Institution",
                newName: "Institutions");

            migrationBuilder.RenameIndex(
                name: "IX_Supervisor_AppUserId",
                table: "Supervisors",
                newName: "IX_Supervisors_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InstitutionAdmin_AppUserId",
                table: "InstitutionAdmins",
                newName: "IX_InstitutionAdmins_AppUserId");

            migrationBuilder.AddColumn<string>(
                name: "InstitutionId",
                table: "Supervisors",
                type: "nvarchar(150)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstitutionId",
                table: "InstitutionAdmins",
                type: "nvarchar(150)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InstitutionAdminId",
                table: "Institutions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Supervisors",
                table: "Supervisors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstitutionAdmins",
                table: "InstitutionAdmins",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Institutions",
                table: "Institutions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Supervisors_InstitutionId",
                table: "Supervisors",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionAdmins_InstitutionId",
                table: "InstitutionAdmins",
                column: "InstitutionId",
                unique: true,
                filter: "[InstitutionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_Institutions_InstitutionId",
                table: "Childrens",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Class_Institutions_InstitutionId",
                table: "Class",
                column: "InstitutionId",
                principalTable: "Institutions",
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
                name: "FK_InstitutionAdmins_AspNetUsers_AppUserId",
                table: "InstitutionAdmins",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstitutionAdmins_Institutions_InstitutionId",
                table: "InstitutionAdmins",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Supervisors_AspNetUsers_AppUserId",
                table: "Supervisors",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Supervisors_Institutions_InstitutionId",
                table: "Supervisors",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Childrens_Institutions_InstitutionId",
                table: "Childrens");

            migrationBuilder.DropForeignKey(
                name: "FK_Class_Institutions_InstitutionId",
                table: "Class");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSupervisor_Supervisors_SupervisorId",
                table: "ClassSupervisor");

            migrationBuilder.DropForeignKey(
                name: "FK_InstitutionAdmins_AspNetUsers_AppUserId",
                table: "InstitutionAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_InstitutionAdmins_Institutions_InstitutionId",
                table: "InstitutionAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_Supervisors_AspNetUsers_AppUserId",
                table: "Supervisors");

            migrationBuilder.DropForeignKey(
                name: "FK_Supervisors_Institutions_InstitutionId",
                table: "Supervisors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Supervisors",
                table: "Supervisors");

            migrationBuilder.DropIndex(
                name: "IX_Supervisors_InstitutionId",
                table: "Supervisors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Institutions",
                table: "Institutions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InstitutionAdmins",
                table: "InstitutionAdmins");

            migrationBuilder.DropIndex(
                name: "IX_InstitutionAdmins_InstitutionId",
                table: "InstitutionAdmins");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Supervisors");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "InstitutionAdmins");

            migrationBuilder.RenameTable(
                name: "Supervisors",
                newName: "Supervisor");

            migrationBuilder.RenameTable(
                name: "Institutions",
                newName: "Institution");

            migrationBuilder.RenameTable(
                name: "InstitutionAdmins",
                newName: "InstitutionAdmin");

            migrationBuilder.RenameIndex(
                name: "IX_Supervisors_AppUserId",
                table: "Supervisor",
                newName: "IX_Supervisor_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InstitutionAdmins_AppUserId",
                table: "InstitutionAdmin",
                newName: "IX_InstitutionAdmin_AppUserId");

            migrationBuilder.AlterColumn<string>(
                name: "InstitutionAdminId",
                table: "Institution",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Supervisor",
                table: "Supervisor",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Institution",
                table: "Institution",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstitutionAdmin",
                table: "InstitutionAdmin",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Institution_InstitutionAdminId",
                table: "Institution",
                column: "InstitutionAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Childrens_Institution_InstitutionId",
                table: "Childrens",
                column: "InstitutionId",
                principalTable: "Institution",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Class_Institution_InstitutionId",
                table: "Class",
                column: "InstitutionId",
                principalTable: "Institution",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSupervisor_Supervisor_SupervisorId",
                table: "ClassSupervisor",
                column: "SupervisorId",
                principalTable: "Supervisor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Institution_InstitutionAdmin_InstitutionAdminId",
                table: "Institution",
                column: "InstitutionAdminId",
                principalTable: "InstitutionAdmin",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstitutionAdmin_AspNetUsers_AppUserId",
                table: "InstitutionAdmin",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Supervisor_AspNetUsers_AppUserId",
                table: "Supervisor",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
