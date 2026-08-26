using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PatientSurvey.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SystemAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    user_role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    request_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_name",
                table: "audit_logs",
                column: "entity_name");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_occurred_at_utc",
                table: "audit_logs",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_username",
                table: "audit_logs",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}
