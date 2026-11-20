using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres;

[DbContext(typeof(PostgresServerDbContext))]
[Migration("20261120190000_UneonTtsVoice")]
public sealed class UneonTtsVoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("tts_voice", "profile", type: "text", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("tts_voice", "profile");
}
