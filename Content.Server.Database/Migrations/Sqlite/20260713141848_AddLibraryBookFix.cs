using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddLibraryBookFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "library_book");

            migrationBuilder.CreateTable(
                name: "nf_library_book",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nf_library_book_id = table.Column<int>(type: "INTEGER", nullable: false),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    author = table.Column<string>(type: "TEXT", nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    author_player_user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nf_library_book", x => x.round_id);
                    table.ForeignKey(
                        name: "FK_nf_library_book_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nf_library_book_server_id",
                table: "nf_library_book",
                column: "server_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nf_library_book");

            migrationBuilder.CreateTable(
                name: "library_book",
                columns: table => new
                {
                    library_book_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    author = table.Column<string>(type: "TEXT", nullable: false),
                    author_ckey = table.Column<string>(type: "TEXT", nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_book", x => x.library_book_id);
                    table.ForeignKey(
                        name: "FK_library_book_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_library_book_server_id",
                table: "library_book",
                column: "server_id");
        }
    }
}
