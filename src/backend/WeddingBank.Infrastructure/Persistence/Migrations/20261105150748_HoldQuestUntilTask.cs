using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HoldQuestUntilTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReleasedByTaskId",
                table: "Quests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_ReleasedByTaskId",
                table: "Quests",
                column: "ReleasedByTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_Tasks_ReleasedByTaskId",
                table: "Quests",
                column: "ReleasedByTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_Tasks_ReleasedByTaskId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_ReleasedByTaskId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "ReleasedByTaskId",
                table: "Quests");
        }
    }
}
