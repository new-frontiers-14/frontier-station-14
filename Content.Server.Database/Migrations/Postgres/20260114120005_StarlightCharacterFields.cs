using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class StarlightCharacterFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "voice",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "silicon_voice",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "custom_specie_name",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cybernetics",
                table: "profile",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "width",
                table: "profile",
                type: "real",
                nullable: false,
                defaultValue: 1f);

            migrationBuilder.AddColumn<float>(
                name: "height",
                table: "profile",
                type: "real",
                nullable: false,
                defaultValue: 1f);

            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "job_priority_entry",
                columns: table => new
                {
                    job_priority_entry_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.SerialColumn),
                    preference_id = table.Column<int>(type: "integer", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_priority_entry", x => x.job_priority_entry_id);
                    table.ForeignKey(
                        name: "FK_job_priority_entry_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO job_priority_entry (preference_id, job_name, priority)
                SELECT pref.preference_id, job_name, priority
                FROM job
                INNER JOIN profile AS prof ON job.profile_id = prof.profile_id
                INNER JOIN preference AS pref ON pref.preference_id = prof.preference_id
                WHERE prof.slot = pref.selected_character_slot
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_job_priority_entry_preference_id",
                table: "job_priority_entry",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_one_high_priority_pref",
                table: "job_priority_entry",
                column: "preference_id",
                unique: true)
                .Annotation("Npgsql:IndexFilter", "priority = 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_priority_entry");

            migrationBuilder.DropColumn(
                name: "voice",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "silicon_voice",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "custom_specie_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "cybernetics",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "width",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "height",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "enabled",
                table: "profile");
        }
    }
}
