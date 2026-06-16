using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class WayfarerSafetyDepositBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wayfarer_safety_deposit_box",
                columns: table => new
                {
                    wayfarer_safety_deposit_box_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    box_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    owner_name = table.Column<string>(type: "TEXT", nullable: false),
                    purchase_date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wayfarer_safety_deposit_box", x => x.wayfarer_safety_deposit_box_id);
                });

            migrationBuilder.CreateTable(
                name: "wayfarer_safety_deposit_box_item",
                columns: table => new
                {
                    wayfarer_safety_deposit_box_item_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    box_id = table.Column<int>(type: "INTEGER", nullable: false),
                    entity_data = table.Column<string>(type: "TEXT", nullable: false),
                    deposit_date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wayfarer_safety_deposit_box_item", x => x.wayfarer_safety_deposit_box_item_id);
                    table.ForeignKey(
                        name: "FK_wayfarer_safety_deposit_box_item_wayfarer_safety_deposit_box_box_id",
                        column: x => x.box_id,
                        principalTable: "wayfarer_safety_deposit_box",
                        principalColumn: "wayfarer_safety_deposit_box_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wayfarer_safety_deposit_box_box_id",
                table: "wayfarer_safety_deposit_box",
                column: "box_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wayfarer_safety_deposit_box_owner_user_id",
                table: "wayfarer_safety_deposit_box",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wayfarer_safety_deposit_box_item_box_id",
                table: "wayfarer_safety_deposit_box_item",
                column: "box_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wayfarer_safety_deposit_box_item");

            migrationBuilder.DropTable(
                name: "wayfarer_safety_deposit_box");
        }
    }
}
