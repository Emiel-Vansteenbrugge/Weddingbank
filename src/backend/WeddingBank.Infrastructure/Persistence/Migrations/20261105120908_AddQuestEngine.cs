using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Audience = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quests", x => x.Id);
                    table.CheckConstraint("CK_Quests_Audience", "\"Audience\" IN ('JUUL', 'AMBER', 'BOTH')");
                    table.CheckConstraint("CK_Quests_Kind", "\"Kind\" IN ('MAIN', 'SIDE')");
                    table.CheckConstraint("CK_Quests_Status", "\"Status\" IN ('DRAFT', 'PUBLISHED', 'ARCHIVED')");
                    table.ForeignKey(
                        name: "FK_Quests_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParticipantLogin = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IgnoreCase = table.Column<bool>(type: "boolean", nullable: false),
                    TrimWhitespace = table.Column<bool>(type: "boolean", nullable: false),
                    SuccessMessage = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.CheckConstraint("CK_Tasks_Scope", "(\"Scope\" = 'SHARED' AND \"ParticipantLogin\" IS NULL) OR (\"Scope\" = 'INDIVIDUAL' AND \"ParticipantLogin\" IN ('Juul-V', 'Amber-D'))");
                    table.CheckConstraint("CK_Tasks_Status", "\"Status\" IN ('DRAFT', 'PUBLISHED')");
                    table.CheckConstraint("CK_Tasks_Type", "\"TaskType\" IN ('TEXT')");
                    table.ForeignKey(
                        name: "FK_Tasks_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentUnlocks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentUnlocks_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAnswers_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId",
                table: "Submissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskId",
                table: "Assignments",
                column: "TaskId",
                unique: true,
                filter: "\"TaskId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUnlocks_DocumentId",
                table: "DocumentUnlocks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUnlocks_TaskId_DocumentId",
                table: "DocumentUnlocks",
                columns: new[] { "TaskId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_CaseId",
                table: "Quests",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_DisplayOrder",
                table: "Quests",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_Reference",
                table: "Quests",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAnswers_TaskId_DisplayOrder",
                table: "TaskAnswers",
                columns: new[] { "TaskId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_QuestId_DisplayOrder",
                table: "Tasks",
                columns: new[] { "QuestId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Reference",
                table: "Tasks",
                column: "Reference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Tasks_TaskId",
                table: "Assignments",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Assignments_AssignmentId",
                table: "Submissions",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                INSERT INTO "Quests" ("Id", "Reference", "Title", "Description", "Instructions", "CaseId", "Audience", "Kind", "Status", "DisplayOrder", "CreatedAt", "UpdatedAt")
                SELECT 'c1a00001-0000-4000-8000-000000000001', 'CH-I-QUEST-001', 'The Missing Record',
                       'An inconsistency has been identified in the records relating to the restricted allocation. Further examination of the October closing register may clarify where the missing supporting documentation was recorded.',
                       'Use the information available in your personal account documents and the October 2026 Closing Register to identify the correct registry entry.',
                       c."Id", 'BOTH', 'MAIN', 'PUBLISHED', 0, TIMESTAMPTZ '2026-11-05 12:00:00+00', TIMESTAMPTZ '2026-11-05 12:00:00+00'
                FROM "Cases" c
                WHERE c."Reference" = 'CH-I-001'
                  AND NOT EXISTS (SELECT 1 FROM "Quests" q WHERE q."Reference" = 'CH-I-QUEST-001');

                INSERT INTO "Tasks" ("Id", "QuestId", "Reference", "Title", "Instructions", "TaskType", "Scope", "ParticipantLogin", "IgnoreCase", "TrimWhitespace", "SuccessMessage", "DisplayOrder", "Status", "CreatedAt", "UpdatedAt")
                SELECT 'c1a00001-0000-4000-8000-000000000002', q."Id", 'CH-I-TASK-001', 'Identify the Registry Entry',
                       'Submit the reference number of the registry entry that matches the department, date and subject identified in the available records.',
                       'TEXT', 'SHARED', NULL, TRUE, TRUE,
                       'The registry entry has been identified. The associated record is now available for examination.',
                       0, 'PUBLISHED', TIMESTAMPTZ '2026-11-05 12:00:00+00', TIMESTAMPTZ '2026-11-05 12:00:00+00'
                FROM "Quests" q
                WHERE q."Reference" = 'CH-I-QUEST-001'
                  AND NOT EXISTS (SELECT 1 FROM "Tasks" t WHERE t."Reference" = 'CH-I-TASK-001');

                INSERT INTO "Assignments" ("Id", "Code", "Scope", "ParticipantLogin", "Completed", "UpdatedAt", "TaskId")
                SELECT 'c1a00001-0000-4000-8000-000000000003', 'CH-I-TASK-001', 'SHARED', NULL, FALSE, TIMESTAMPTZ '2026-11-05 12:00:00+00', t."Id"
                FROM "Tasks" t
                WHERE t."Reference" = 'CH-I-TASK-001'
                  AND NOT EXISTS (SELECT 1 FROM "Assignments" a WHERE a."TaskId" = t."Id");

                INSERT INTO "TaskAnswers" ("Id", "TaskId", "Text", "DisplayOrder")
                SELECT 'c1a00001-0000-4000-8000-000000000004', t."Id", 'REG-1026-17', 0
                FROM "Tasks" t
                WHERE t."Reference" = 'CH-I-TASK-001'
                  AND NOT EXISTS (SELECT 1 FROM "TaskAnswers" a WHERE a."TaskId" = t."Id");

                INSERT INTO "DocumentUnlocks" ("Id", "TaskId", "DocumentId")
                SELECT 'c1a00001-0000-4000-8000-000000000005', t."Id", d."Id"
                FROM "Tasks" t
                JOIN "Quests" q ON q."Id" = t."QuestId"
                JOIN "Documents" d ON d."CaseId" = q."CaseId" AND d."Reference" = 'CH-I-DOC-007'
                WHERE t."Reference" = 'CH-I-TASK-001'
                  AND NOT EXISTS (SELECT 1 FROM "DocumentUnlocks" u WHERE u."TaskId" = t."Id" AND u."DocumentId" = d."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Submissions" s
                USING "Assignments" a
                JOIN "Tasks" t ON t."Id" = a."TaskId"
                WHERE s."AssignmentId" = a."Id" AND t."Reference" = 'CH-I-TASK-001';

                DELETE FROM "Assignments" a
                USING "Tasks" t
                WHERE a."TaskId" = t."Id" AND t."Reference" = 'CH-I-TASK-001';

                DELETE FROM "DocumentUnlocks" u
                USING "Tasks" t
                WHERE u."TaskId" = t."Id" AND t."Reference" = 'CH-I-TASK-001';

                DELETE FROM "TaskAnswers" a
                USING "Tasks" t
                WHERE a."TaskId" = t."Id" AND t."Reference" = 'CH-I-TASK-001';

                DELETE FROM "Tasks" WHERE "Reference" = 'CH-I-TASK-001';
                DELETE FROM "Quests" WHERE "Reference" = 'CH-I-QUEST-001';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Tasks_TaskId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Assignments_AssignmentId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "DocumentUnlocks");

            migrationBuilder.DropTable(
                name: "TaskAnswers");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignmentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TaskId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Assignments");
        }
    }
}
