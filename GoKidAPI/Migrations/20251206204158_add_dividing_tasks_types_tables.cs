using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_dividing_tasks_types_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_TaskCategories_TaskCategoryId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_TaskCategoryId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "MaxVoiceAttempts",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "MaxVoiceDurationSeconds",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "TaskCategoryId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "VerificationType",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "AssignedByWeeklyAdventureId",
                table: "ChildTasks");

            migrationBuilder.RenameColumn(
                name: "VoicePrompt",
                table: "TaskTemplates",
                newName: "IconPublicId");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "ChildTasks",
                newName: "AnswerText");

            migrationBuilder.RenameColumn(
                name: "LastVoiceResult",
                table: "ChildTasks",
                newName: "AnswerMediaUrl");

            migrationBuilder.AddColumn<Guid>(
                name: "SubCategoryId",
                table: "TaskTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TemplateType",
                table: "TaskTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ChildTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EvidenceSubmissionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    InstructionsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskImagePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    ReviewBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceSubmissionTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceSubmissionTask_TaskTemplates_Id",
                        column: x => x.Id,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstantRewardTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(150)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstantRewardTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstantRewardTask_TaskTemplates_Id",
                        column: x => x.Id,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TextQuestionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskImagePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedCorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CaseSensitive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextQuestionTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TextQuestionTask_TaskTemplates_Id",
                        column: x => x.Id,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceQuestionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskImagePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedCorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VoicePrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxVoiceAttempts = table.Column<int>(type: "int", nullable: false),
                    MaxVoiceDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    UseAIValidation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceQuestionTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceQuestionTask_TaskTemplates_Id",
                        column: x => x.Id,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChildTasks_TaskTemplates_TemplateId",
                table: "ChildTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_SubCategories_CategoryId",
                table: "TaskTemplates");

            migrationBuilder.DropTable(
                name: "EvidenceSubmissionTask");

            migrationBuilder.DropTable(
                name: "InstantRewardTask");

            migrationBuilder.DropTable(
                name: "TextQuestionTask");

            migrationBuilder.DropTable(
                name: "VoiceQuestionTask");

            migrationBuilder.DropColumn(
                name: "TemplateType",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ChildTasks");

            migrationBuilder.RenameColumn(
                name: "IconPublicId",
                table: "TaskTemplates",
                newName: "VoicePrompt");

            migrationBuilder.RenameColumn(
                name: "AnswerText",
                table: "ChildTasks",
                newName: "MediaUrl");

            migrationBuilder.RenameColumn(
                name: "AnswerMediaUrl",
                table: "ChildTasks",
                newName: "LastVoiceResult");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TaskTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxVoiceAttempts",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxVoiceDurationSeconds",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "VerificationType",
                table: "TaskTemplates",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<string>(
                name: "TaskTemplateId",
                table: "ChildTasks",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AssignedByWeeklyAdventureId",
                table: "ChildTasks",
                type: "nvarchar(max)",
                nullable: true);
            }
    }
}
