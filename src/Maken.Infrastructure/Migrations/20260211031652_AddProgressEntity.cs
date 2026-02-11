using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maken.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Progresses_Lessons_LessonId",
                table: "Progresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Progresses_Users_StudentId",
                table: "Progresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Progresses",
                table: "Progresses");

            migrationBuilder.DropIndex(
                name: "IX_Progresses_StudentId",
                table: "Progresses");

            migrationBuilder.RenameTable(
                name: "Progresses",
                newName: "Progress");

            migrationBuilder.RenameIndex(
                name: "IX_Progresses_LessonId",
                table: "Progress",
                newName: "IX_Progress_LessonId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Progress",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Progress",
                table: "Progress",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Progress_StudentId_LessonId",
                table: "Progress",
                columns: new[] { "StudentId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Progress_TenantId",
                table: "Progress",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Progress_Lessons_LessonId",
                table: "Progress",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Progress_Users_StudentId",
                table: "Progress",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Progress_Lessons_LessonId",
                table: "Progress");

            migrationBuilder.DropForeignKey(
                name: "FK_Progress_Users_StudentId",
                table: "Progress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Progress",
                table: "Progress");

            migrationBuilder.DropIndex(
                name: "IX_Progress_StudentId_LessonId",
                table: "Progress");

            migrationBuilder.DropIndex(
                name: "IX_Progress_TenantId",
                table: "Progress");

            migrationBuilder.RenameTable(
                name: "Progress",
                newName: "Progresses");

            migrationBuilder.RenameIndex(
                name: "IX_Progress_LessonId",
                table: "Progresses",
                newName: "IX_Progresses_LessonId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Progresses",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Progresses",
                table: "Progresses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Progresses_StudentId",
                table: "Progresses",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Progresses_Lessons_LessonId",
                table: "Progresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Progresses_Users_StudentId",
                table: "Progresses",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
