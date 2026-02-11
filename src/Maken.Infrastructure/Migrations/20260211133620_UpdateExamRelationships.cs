using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maken.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExamRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Choices_Questions_QuestionId",
                table: "Choices");

            migrationBuilder.DropForeignKey(
                name: "FK_Choices_Questions_QuestionId1",
                table: "Choices");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Exams_ExamId1",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ExamId1",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Choices_QuestionId1",
                table: "Choices");

            migrationBuilder.DropColumn(
                name: "ExamId1",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionId1",
                table: "Choices");

            migrationBuilder.AddForeignKey(
                name: "FK_Choices_Questions_QuestionId",
                table: "Choices",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Choices_Questions_QuestionId",
                table: "Choices");

            migrationBuilder.AddColumn<Guid>(
                name: "ExamId1",
                table: "Questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionId1",
                table: "Choices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId1",
                table: "Questions",
                column: "ExamId1");

            migrationBuilder.CreateIndex(
                name: "IX_Choices_QuestionId1",
                table: "Choices",
                column: "QuestionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Choices_Questions_QuestionId",
                table: "Choices",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Choices_Questions_QuestionId1",
                table: "Choices",
                column: "QuestionId1",
                principalTable: "Questions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Exams_ExamId1",
                table: "Questions",
                column: "ExamId1",
                principalTable: "Exams",
                principalColumn: "Id");
        }
    }
}
