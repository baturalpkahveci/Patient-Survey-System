using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PatientSurvey.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PatientSpecificInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "department_id",
                table: "surveys",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "doctor_id",
                table: "surveys",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "department_id",
                table: "survey_responses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "survey_invitation_id",
                table: "survey_access_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "doctors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctors", x => x.id);
                    table.ForeignKey(
                        name: "FK_doctors_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctors_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tc_identity_lookup_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient_visits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    doctor_id = table.Column<int>(type: "integer", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    examined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_visits", x => x.id);
                    table.CheckConstraint("ck_patient_visits_doctor_department_pair", "(doctor_id IS NULL AND department_id IS NULL) OR (doctor_id IS NOT NULL AND department_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_patient_visits_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_visits_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_visits_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_visits_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_invitations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    survey_id = table.Column<int>(type: "integer", nullable: false),
                    patient_visit_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_method = table.Column<int>(type: "integer", nullable: false),
                    delivery_status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_invitations_patient_visits_patient_visit_id",
                        column: x => x.patient_visit_id,
                        principalTable: "patient_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_invitations_surveys_survey_id",
                        column: x => x.survey_id,
                        principalTable: "surveys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_invitations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_consents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    survey_invitation_id = table.Column<int>(type: "integer", nullable: false),
                    notice_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_consents", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_consents_survey_invitations_survey_invitation_id",
                        column: x => x.survey_invitation_id,
                        principalTable: "survey_invitations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "is_active", "name" },
                values: new object[] { 3, true, "Doctor" });

            migrationBuilder.CreateIndex(
                name: "IX_surveys_department_id",
                table: "surveys",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_surveys_doctor_id",
                table: "surveys",
                column: "doctor_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_surveys_doctor_department_pair",
                table: "surveys",
                sql: "(doctor_id IS NULL AND department_id IS NULL) OR (doctor_id IS NOT NULL AND department_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_survey_access_tokens_survey_invitation_id",
                table: "survey_access_tokens",
                column: "survey_invitation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctors_department_id",
                table: "doctors",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_user_id",
                table: "doctors",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_visits_created_by_user_id",
                table: "patient_visits",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_patient_visits_department_id",
                table: "patient_visits",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_patient_visits_doctor_id",
                table: "patient_visits",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_patient_visits_patient_id",
                table: "patient_visits",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_tc_identity_lookup_hash",
                table: "patients",
                column: "tc_identity_lookup_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_consents_survey_invitation_id",
                table: "survey_consents",
                column: "survey_invitation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_invitations_created_by_user_id",
                table: "survey_invitations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_invitations_patient_visit_id",
                table: "survey_invitations",
                column: "patient_visit_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_invitations_survey_id",
                table: "survey_invitations",
                column: "survey_id");

            migrationBuilder.AddForeignKey(
                name: "FK_survey_access_tokens_survey_invitations_survey_invitation_id",
                table: "survey_access_tokens",
                column: "survey_invitation_id",
                principalTable: "survey_invitations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_surveys_departments_department_id",
                table: "surveys",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_surveys_doctors_doctor_id",
                table: "surveys",
                column: "doctor_id",
                principalTable: "doctors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_survey_access_tokens_survey_invitations_survey_invitation_id",
                table: "survey_access_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_surveys_departments_department_id",
                table: "surveys");

            migrationBuilder.DropForeignKey(
                name: "FK_surveys_doctors_doctor_id",
                table: "surveys");

            migrationBuilder.DropTable(
                name: "survey_consents");

            migrationBuilder.DropTable(
                name: "survey_invitations");

            migrationBuilder.DropTable(
                name: "patient_visits");

            migrationBuilder.DropTable(
                name: "doctors");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropIndex(
                name: "IX_surveys_department_id",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "IX_surveys_doctor_id",
                table: "surveys");

            migrationBuilder.DropCheckConstraint(
                name: "ck_surveys_doctor_department_pair",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "IX_survey_access_tokens_survey_invitation_id",
                table: "survey_access_tokens");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "doctor_id",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "survey_invitation_id",
                table: "survey_access_tokens");

            migrationBuilder.AlterColumn<int>(
                name: "department_id",
                table: "survey_responses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
