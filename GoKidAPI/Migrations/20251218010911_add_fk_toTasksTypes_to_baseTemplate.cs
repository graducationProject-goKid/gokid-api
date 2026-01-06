using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoKidAPI.Migrations
{
    /// <inheritdoc />
    public partial class add_fk_toTasksTypes_to_baseTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskTemplateBaseId",
                table: "VoiceQuestionTask",
                type: "nvarchar(150)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskTemplateBaseId",
                table: "TextQuestionTask",
                type: "nvarchar(150)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskTemplateBaseId",
                table: "InstantRewardTask",
                type: "nvarchar(150)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskTemplateBaseId",
                table: "EvidenceSubmissionTask",
                type: "nvarchar(150)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceQuestionTask_TaskTemplateBaseId",
                table: "VoiceQuestionTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TextQuestionTask_TaskTemplateBaseId",
                table: "TextQuestionTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstantRewardTask_TaskTemplateBaseId",
                table: "InstantRewardTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceSubmissionTask_TaskTemplateBaseId",
                table: "EvidenceSubmissionTask",
                column: "TaskTemplateBaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceSubmissionTask_TaskTemplates_TaskTemplateBaseId",
                table: "EvidenceSubmissionTask",
                column: "TaskTemplateBaseId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InstantRewardTask_TaskTemplates_TaskTemplateBaseId",
                table: "InstantRewardTask",
                column: "TaskTemplateBaseId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TextQuestionTask_TaskTemplates_TaskTemplateBaseId",
                table: "TextQuestionTask",
                column: "TaskTemplateBaseId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VoiceQuestionTask_TaskTemplates_TaskTemplateBaseId",
                table: "VoiceQuestionTask",
                column: "TaskTemplateBaseId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceSubmissionTask_TaskTemplates_TaskTemplateBaseId",
                table: "EvidenceSubmissionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_InstantRewardTask_TaskTemplates_TaskTemplateBaseId",
                table: "InstantRewardTask");

            migrationBuilder.DropForeignKey(
                name: "FK_TextQuestionTask_TaskTemplates_TaskTemplateBaseId",
                table: "TextQuestionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_VoiceQuestionTask_TaskTemplates_TaskTemplateBaseId",
                table: "VoiceQuestionTask");

            migrationBuilder.DropIndex(
                name: "IX_VoiceQuestionTask_TaskTemplateBaseId",
                table: "VoiceQuestionTask");

            migrationBuilder.DropIndex(
                name: "IX_TextQuestionTask_TaskTemplateBaseId",
                table: "TextQuestionTask");

            migrationBuilder.DropIndex(
                name: "IX_InstantRewardTask_TaskTemplateBaseId",
                table: "InstantRewardTask");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceSubmissionTask_TaskTemplateBaseId",
                table: "EvidenceSubmissionTask");

            migrationBuilder.DropColumn(
                name: "TaskTemplateBaseId",
                table: "VoiceQuestionTask");

            migrationBuilder.DropColumn(
                name: "TaskTemplateBaseId",
                table: "TextQuestionTask");

            migrationBuilder.DropColumn(
                name: "TaskTemplateBaseId",
                table: "InstantRewardTask");

            migrationBuilder.DropColumn(
                name: "TaskTemplateBaseId",
                table: "EvidenceSubmissionTask");
        }
    }
}
