using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class fix_supervisor_appuser_relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill existing rows so ClassSupervisors keeps pointing at the right Supervisor
            // before Supervisors.Id is repointed from its own random GUID to AppUserId.
            migrationBuilder.Sql(@"
                ALTER TABLE ClassSupervisors NOCHECK CONSTRAINT ALL;

                UPDATE cs
                SET cs.SupervisorId = s.AppUserId
                FROM ClassSupervisors cs
                INNER JOIN Supervisors s ON cs.SupervisorId = s.Id;

                UPDATE Supervisors
                SET Id = AppUserId;

                ALTER TABLE ClassSupervisors WITH CHECK CHECK CONSTRAINT ALL;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Supervisors_AspNetUsers_AppUserId",
                table: "Supervisors");

            migrationBuilder.DropIndex(
                name: "IX_Supervisors_AppUserId",
                table: "Supervisors");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Supervisors");

            migrationBuilder.AddForeignKey(
                name: "FK_Supervisors_AspNetUsers_Id",
                table: "Supervisors",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supervisors_AspNetUsers_Id",
                table: "Supervisors");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Supervisors",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Supervisors_AppUserId",
                table: "Supervisors",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Supervisors_AspNetUsers_AppUserId",
                table: "Supervisors",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
