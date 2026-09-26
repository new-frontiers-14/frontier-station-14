using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddNicknameAndBoxSizeToSafetyDepositBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "box_size",
                table: "wayfarer_safety_deposit_box",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "wayfarer_safety_deposit_box",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "box_size",
                table: "wayfarer_safety_deposit_box");

            migrationBuilder.DropColumn(
                name: "nickname",
                table: "wayfarer_safety_deposit_box");
        }
    }
}
