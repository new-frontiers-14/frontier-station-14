using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261120190000_UneonTtsVoice")]
public sealed class UneonTtsVoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("tts_voice", "profile", type: "TEXT", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("ALTER TABLE profile DROP COLUMN tts_voice");
}
