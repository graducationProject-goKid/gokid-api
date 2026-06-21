using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
/// <summary>
/// This migration refactors the Adventures module to improve database design,
/// normalize relationships, and enhance tracking capabilities.
///
/// Key Changes:
///
/// 1. Primary Key Refactoring:
///    - Replaced composite primary keys with single-column primary keys (Id) in:
///      • ChildAdventureProgress
///      • AdventureTask
///    - This simplifies relationships, improves query performance, and reduces complexity.
///
/// 2. Adventure Progress Restructuring:
///    - Removed legacy JSON-based tracking (DayCompletionJson).
///    - Introduced structured progress fields:
///      • EarnedStars
///      • EarnedPoints
///      • CompletedDaysCount
///      • IsCompleted
///      • CompletedAt
///
/// 3. WeeklyAdventure Enhancements:
///    - Added EndDate to define adventure duration.
///    - Added Status to manage lifecycle (e.g., Active, Completed).
///
/// 4. AdventureTask Enhancements:
///    - Added Id as primary key.
///    - Added Stars (reward system).
///    - Added Story content support:
///      • StoryText
///      • StoryVoiceUrl
///      • StoryVoicePublicId
///
/// 5. Adventure Entity Improvements:
///    - Made Description required.
///    - Added DescriptionVoiceUrl for audio support.
///    - Added InstitutionId to scope adventures.
///    - Added Status for lifecycle management.
///
/// 6. Introduced ChildAdventureTask Table:
///    - Represents a child's execution of an adventure task.
///    - Establishes relationships with:
///      • Child
///      • AdventureTask
///      • WeeklyAdventure
///      • ChildAdventureProgress
///    - Tracks:
///      • Task status
///      • Earned stars
///      • Submission and completion timestamps
///      • Supervisor review data
///      • Evidence (media)
///
/// 7. Relationship Simplification:
///    - Replaced previous composite foreign keys with a single foreign key:
///      • ChildAdventureTask → ChildAdventureProgress (via Id)
///
/// 8. Indexing:
///    - Added indexes on key foreign keys to improve query performance:
///      • ChildId
///      • AdventureTaskId
///      • WeeklyAdventureId
///      • ChildAdventureProgressId
///
/// Overall Impact:
///    - Cleaner schema with simplified relationships
///    - Better scalability and maintainability
///    - Improved performance and query efficiency
///    - Production-ready structure for the Adventures system
/// </summary>


namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class modified_adventures_models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete the Composite PKs from the adventureProgress
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildAdventureProgress",
                table: "ChildAdventureProgress");
            // Delete the Composite PKs from the AdventureTask
            migrationBuilder.DropPrimaryKey(
                name: "PK_AdventureTask",
                table: "AdventureTask");

            // Deleted cause we have the ChildAdventureTask
            migrationBuilder.DropColumn(
                name: "DayCompletionJson",
                table: "ChildAdventureProgress");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "WeeklyAdventure",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WeeklyAdventure",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Create the new PK for this table (after deleting the old compositePK)
            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "ChildAdventureProgress",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ChildAdventureProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedDaysCount",
                table: "ChildAdventureProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EarnedPoints",
                table: "ChildAdventureProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EarnedStars",
                table: "ChildAdventureProgress",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ChildAdventureProgress",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "AdventureTask",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Stars",
                table: "AdventureTask",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoryText",
                table: "AdventureTask",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryVoicePublicId",
                table: "AdventureTask",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryVoiceUrl",
                table: "AdventureTask",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Adventure",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionVoiceUrl",
                table: "Adventure",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionId",
                table: "Adventure",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Adventure",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildAdventureProgress",
                table: "ChildAdventureProgress",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdventureTask",
                table: "AdventureTask",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ChildAdventureTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChildId = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    AdventureTaskId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WeeklyAdventureId = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    ChildAdventureProgressId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EarnedStars = table.Column<int>(type: "int", nullable: false),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildAdventureTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildAdventureTask_AdventureTask_AdventureTaskId",
                        column: x => x.AdventureTaskId,
                        principalTable: "AdventureTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildAdventureTask_ChildAdventureProgress_ChildAdventureProgressId",
                        column: x => x.ChildAdventureProgressId,
                        principalTable: "ChildAdventureProgress",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChildAdventureTask_Childrens_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Childrens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildAdventureTask_WeeklyAdventure_WeeklyAdventureId",
                        column: x => x.WeeklyAdventureId,
                        principalTable: "WeeklyAdventure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildAdventureProgress_WeeklyAdventureId",
                table: "ChildAdventureProgress",
                column: "WeeklyAdventureId");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureTask_AdventureId",
                table: "AdventureTask",
                column: "AdventureId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildAdventureTask_AdventureTaskId",
                table: "ChildAdventureTask",
                column: "AdventureTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildAdventureTask_ChildAdventureProgressId",
                table: "ChildAdventureTask",
                column: "ChildAdventureProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildAdventureTask_ChildId",
                table: "ChildAdventureTask",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildAdventureTask_WeeklyAdventureId",
                table: "ChildAdventureTask",
                column: "WeeklyAdventureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildAdventureTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChildAdventureProgress",
                table: "ChildAdventureProgress");

            migrationBuilder.DropIndex(
                name: "IX_ChildAdventureProgress_WeeklyAdventureId",
                table: "ChildAdventureProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdventureTask",
                table: "AdventureTask");

            migrationBuilder.DropIndex(
                name: "IX_AdventureTask_AdventureId",
                table: "AdventureTask");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "WeeklyAdventure");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WeeklyAdventure");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ChildAdventureProgress");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ChildAdventureProgress");

            migrationBuilder.DropColumn(
                name: "CompletedDaysCount",
                table: "ChildAdventureProgress");

            migrationBuilder.DropColumn(
                name: "EarnedPoints",
                table: "ChildAdventureProgress");

            migrationBuilder.DropColumn(
                name: "EarnedStars",
                table: "ChildAdventureProgress");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ChildAdventureProgress");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AdventureTask");

            migrationBuilder.DropColumn(
                name: "Stars",
                table: "AdventureTask");

            migrationBuilder.DropColumn(
                name: "StoryText",
                table: "AdventureTask");

            migrationBuilder.DropColumn(
                name: "StoryVoicePublicId",
                table: "AdventureTask");

            migrationBuilder.DropColumn(
                name: "StoryVoiceUrl",
                table: "AdventureTask");

            migrationBuilder.DropColumn(
                name: "DescriptionVoiceUrl",
                table: "Adventure");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Adventure");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Adventure");

            migrationBuilder.AddColumn<string>(
                name: "DayCompletionJson",
                table: "ChildAdventureProgress",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Adventure",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChildAdventureProgress",
                table: "ChildAdventureProgress",
                columns: new[] { "WeeklyAdventureId", "ChildId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdventureTask",
                table: "AdventureTask",
                columns: new[] { "AdventureId", "TaskTemplateId" });
        }
    }
}
