using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameParticipantLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Submissions_Outcome",
                table: "Submissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneCalls_Participant",
                table: "PhoneCalls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notices_Audience",
                table: "Notices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assignments_Scope",
                table: "Assignments");

            migrationBuilder.Sql("""
                UPDATE "Users" SET "Login" = 'Juul-V' WHERE "Login" = 'juul';
                UPDATE "Users" SET "Login" = 'Amber-D' WHERE "Login" = 'amber';
                UPDATE "Assignments" SET "ParticipantLogin" = 'Juul-V' WHERE "ParticipantLogin" = 'juul';
                UPDATE "Assignments" SET "ParticipantLogin" = 'Amber-D' WHERE "ParticipantLogin" = 'amber';
                UPDATE "Notices" SET "ParticipantLogin" = 'Juul-V' WHERE "ParticipantLogin" = 'juul';
                UPDATE "Notices" SET "ParticipantLogin" = 'Amber-D' WHERE "ParticipantLogin" = 'amber';
                UPDATE "PhoneCalls" SET "ParticipantLogin" = 'Juul-V' WHERE "ParticipantLogin" = 'juul';
                UPDATE "PhoneCalls" SET "ParticipantLogin" = 'Amber-D' WHERE "ParticipantLogin" = 'amber';
                UPDATE "Activities" SET "ParticipantLogin" = 'Juul-V' WHERE "ParticipantLogin" = 'juul';
                UPDATE "Activities" SET "ParticipantLogin" = 'Amber-D' WHERE "ParticipantLogin" = 'amber';
                UPDATE "Submissions" SET "ParticipantLogin" = 'Juul-V' WHERE "ParticipantLogin" = 'juul';
                UPDATE "Submissions" SET "ParticipantLogin" = 'Amber-D' WHERE "ParticipantLogin" = 'amber';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Submissions_Outcome",
                table: "Submissions",
                sql: "\"Outcome\" IN ('PENDING', 'ACCEPTED', 'REFUSED') AND \"ParticipantLogin\" IN ('Juul-V', 'Amber-D')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneCalls_Participant",
                table: "PhoneCalls",
                sql: "\"ParticipantLogin\" IN ('Juul-V', 'Amber-D')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notices_Audience",
                table: "Notices",
                sql: "\"Kind\" IN ('HINT', 'MESSAGE') AND (\"ParticipantLogin\" IS NULL OR \"ParticipantLogin\" IN ('Juul-V', 'Amber-D'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assignments_Scope",
                table: "Assignments",
                sql: "(\"Scope\" = 'SHARED' AND \"ParticipantLogin\" IS NULL) OR (\"Scope\" = 'INDIVIDUAL' AND \"ParticipantLogin\" IN ('Juul-V', 'Amber-D'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Submissions_Outcome",
                table: "Submissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneCalls_Participant",
                table: "PhoneCalls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notices_Audience",
                table: "Notices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assignments_Scope",
                table: "Assignments");

            migrationBuilder.Sql("""
                UPDATE "Users" SET "Login" = 'juul' WHERE "Login" = 'Juul-V';
                UPDATE "Users" SET "Login" = 'amber' WHERE "Login" = 'Amber-D';
                UPDATE "Assignments" SET "ParticipantLogin" = 'juul' WHERE "ParticipantLogin" = 'Juul-V';
                UPDATE "Assignments" SET "ParticipantLogin" = 'amber' WHERE "ParticipantLogin" = 'Amber-D';
                UPDATE "Notices" SET "ParticipantLogin" = 'juul' WHERE "ParticipantLogin" = 'Juul-V';
                UPDATE "Notices" SET "ParticipantLogin" = 'amber' WHERE "ParticipantLogin" = 'Amber-D';
                UPDATE "PhoneCalls" SET "ParticipantLogin" = 'juul' WHERE "ParticipantLogin" = 'Juul-V';
                UPDATE "PhoneCalls" SET "ParticipantLogin" = 'amber' WHERE "ParticipantLogin" = 'Amber-D';
                UPDATE "Activities" SET "ParticipantLogin" = 'juul' WHERE "ParticipantLogin" = 'Juul-V';
                UPDATE "Activities" SET "ParticipantLogin" = 'amber' WHERE "ParticipantLogin" = 'Amber-D';
                UPDATE "Submissions" SET "ParticipantLogin" = 'juul' WHERE "ParticipantLogin" = 'Juul-V';
                UPDATE "Submissions" SET "ParticipantLogin" = 'amber' WHERE "ParticipantLogin" = 'Amber-D';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Submissions_Outcome",
                table: "Submissions",
                sql: "\"Outcome\" IN ('PENDING', 'ACCEPTED', 'REFUSED') AND \"ParticipantLogin\" IN ('juul', 'amber')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneCalls_Participant",
                table: "PhoneCalls",
                sql: "\"ParticipantLogin\" IN ('juul', 'amber')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notices_Audience",
                table: "Notices",
                sql: "\"Kind\" IN ('HINT', 'MESSAGE') AND (\"ParticipantLogin\" IS NULL OR \"ParticipantLogin\" IN ('juul', 'amber'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assignments_Scope",
                table: "Assignments",
                sql: "(\"Scope\" = 'SHARED' AND \"ParticipantLogin\" IS NULL) OR (\"Scope\" = 'INDIVIDUAL' AND \"ParticipantLogin\" IN ('juul', 'amber'))");
        }
    }
}
