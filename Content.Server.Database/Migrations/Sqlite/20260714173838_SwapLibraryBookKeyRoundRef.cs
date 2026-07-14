using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SwapLibraryBookKeyRoundRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_nf_library_book",
                table: "nf_library_book");

            migrationBuilder.AlterColumn<int>(
                name: "nf_library_book_id",
                table: "nf_library_book",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "round_id",
                table: "nf_library_book",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_nf_library_book",
                table: "nf_library_book",
                column: "nf_library_book_id");

            migrationBuilder.CreateIndex(
                name: "IX_nf_library_book_round_id",
                table: "nf_library_book",
                column: "round_id");

            migrationBuilder.AddForeignKey(
                name: "FK_nf_library_book_round_round_id",
                table: "nf_library_book",
                column: "round_id",
                principalTable: "round",
                principalColumn: "round_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nf_library_book_round_round_id",
                table: "nf_library_book");

            migrationBuilder.DropPrimaryKey(
                name: "PK_nf_library_book",
                table: "nf_library_book");

            migrationBuilder.DropIndex(
                name: "IX_nf_library_book_round_id",
                table: "nf_library_book");

            migrationBuilder.AlterColumn<int>(
                name: "round_id",
                table: "nf_library_book",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "nf_library_book_id",
                table: "nf_library_book",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_nf_library_book",
                table: "nf_library_book",
                column: "round_id");
        }
    }
}
