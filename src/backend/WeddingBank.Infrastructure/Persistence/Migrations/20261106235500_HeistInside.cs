using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingBank.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(WeddingBankDbContext))]
    [Migration("20261106235500_HeistInside")]
    public partial class HeistInside : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HeistRooms_Stage",
                table: "HeistRooms");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HeistRooms_Stage",
                table: "HeistRooms",
                sql: "\"Stage\" IN ('entrance', 'inside')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HeistRooms_Stage",
                table: "HeistRooms");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HeistRooms_Stage",
                table: "HeistRooms",
                sql: "\"Stage\" = 'entrance'");
        }
    }
}
