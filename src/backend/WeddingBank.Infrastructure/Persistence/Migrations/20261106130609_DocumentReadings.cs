using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DocumentReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentReadBaselines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    EstablishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReadBaselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReadings", x => x.Id);
                    table.CheckConstraint("CK_DocumentReadings_Participant", "\"ParticipantLogin\" IN ('Juul-V', 'Amber-D')");
                    table.ForeignKey(
                        name: "FK_DocumentReadings_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReadings_DocumentId_ParticipantLogin",
                table: "DocumentReadings",
                columns: new[] { "DocumentId", "ParticipantLogin" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentReadBaselines");

            migrationBuilder.DropTable(
                name: "DocumentReadings");
        }
    }
}
