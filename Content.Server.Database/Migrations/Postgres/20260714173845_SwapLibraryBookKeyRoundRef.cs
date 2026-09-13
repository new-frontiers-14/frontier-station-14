using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "round_id",
                table: "nf_library_book",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "nf_library_book_id",
                table: "nf_library_book",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_nf_library_book",
                table: "nf_library_book",
                column: "round_id");
        }
    }
}
