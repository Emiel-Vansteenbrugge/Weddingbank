using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropClientDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientUsers");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Summary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.CheckConstraint("CK_Assignments_Scope", "(\"Scope\" = 'SHARED' AND \"ParticipantLogin\" IS NULL) OR (\"Scope\" = 'INDIVIDUAL' AND \"ParticipantLogin\" IN ('juul', 'amber'))");
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                    table.CheckConstraint("CK_Notices_Audience", "\"Kind\" IN ('HINT', 'MESSAGE') AND (\"ParticipantLogin\" IS NULL OR \"ParticipantLogin\" IN ('juul', 'amber'))");
                });

            migrationBuilder.CreateTable(
                name: "PhoneCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Handled = table.Column<bool>(type: "boolean", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HandledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneCalls", x => x.Id);
                    table.CheckConstraint("CK_PhoneCalls_Participant", "\"ParticipantLogin\" IN ('juul', 'amber')");
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.CheckConstraint("CK_Submissions_Outcome", "\"Outcome\" IN ('PENDING', 'ACCEPTED', 'REFUSED') AND \"ParticipantLogin\" IN ('juul', 'amber')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_OccurredAt",
                table: "Activities",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Scope_ParticipantLogin_Code",
                table: "Assignments",
                columns: new[] { "Scope", "ParticipantLogin", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Outcome",
                table: "Submissions",
                column: "Outcome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Notices");

            migrationBuilder.DropTable(
                name: "PhoneCalls");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    City = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ClientReference = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    EmailAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.CheckConstraint("CK_Clients_Status", "\"Status\" IN ('ACTIVE', 'INACTIVE', 'SUSPENDED')");
                });

            migrationBuilder.CreateTable(
                name: "ClientUsers",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientUsers", x => new { x.ClientId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ClientUsers_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientReference",
                table: "Clients",
                column: "ClientReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CreatedAt",
                table: "Clients",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClientUsers_UserId",
                table: "ClientUsers",
                column: "UserId");
        }
    }
}
