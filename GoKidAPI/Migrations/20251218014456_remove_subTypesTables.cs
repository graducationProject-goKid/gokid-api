using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class remove_subTypesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenceSubmissionTask");

            migrationBuilder.DropTable(
                name: "InstantRewardTask");

            migrationBuilder.DropTable(
                name: "TextQuestionTask");

            migrationBuilder.DropTable(
                name: "VoiceQuestionTask");

            migrationBuilder.AddColumn<bool>(
                name: "CaseSensitive",
                table: "TaskTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceType",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedCorrectAnswer",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstructionsText",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxVoiceAttempts",
                table: "TaskTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxVoiceDurationSeconds",
                table: "TaskTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QuestionText",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewBy",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskImagePublicId",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskImageUrl",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceExpectedCorrectAnswer",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoicePrompt",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceQuestionText",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseSensitive",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "EvidenceType",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "ExpectedCorrectAnswer",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "InstructionsText",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "MaxVoiceAttempts",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "MaxVoiceDurationSeconds",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "QuestionText",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "ReviewBy",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "TaskImagePublicId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "TaskImageUrl",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "VoiceExpectedCorrectAnswer",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "VoicePrompt",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "VoiceQuestionText",
                table: "TaskTemplates");

            migrationBuilder.CreateTable(
                name: "EvidenceSubmissionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaskTemplateBaseId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstructionsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskImagePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_EvidenceSubmissionTask_TaskTemplates_TaskTemplateBaseId",
                        column: x => x.TaskTemplateBaseId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstantRewardTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaskTemplateBaseId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_InstantRewardTask_TaskTemplates_TaskTemplateBaseId",
                        column: x => x.TaskTemplateBaseId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TextQuestionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaskTemplateBaseId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CaseSensitive = table.Column<bool>(type: "bit", nullable: false),
                    ExpectedCorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskImagePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_TextQuestionTask_TaskTemplates_TaskTemplateBaseId",
                        column: x => x.TaskTemplateBaseId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceQuestionTask",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaskTemplateBaseId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpectedCorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxVoiceAttempts = table.Column<int>(type: "int", nullable: false),
                    MaxVoiceDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskImagePublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoicePrompt = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_VoiceQuestionTask_TaskTemplates_TaskTemplateBaseId",
                        column: x => x.TaskTemplateBaseId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceSubmissionTask_TaskTemplateBaseId",
                table: "EvidenceSubmissionTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstantRewardTask_TaskTemplateBaseId",
                table: "InstantRewardTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TextQuestionTask_TaskTemplateBaseId",
                table: "TextQuestionTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceQuestionTask_TaskTemplateBaseId",
                table: "VoiceQuestionTask",
                column: "TaskTemplateBaseId");
        }
    }
}
