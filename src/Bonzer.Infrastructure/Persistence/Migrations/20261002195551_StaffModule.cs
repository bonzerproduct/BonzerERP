using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bonzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StaffModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_staff_tenant_id",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "previous_employer",
                table: "staff");

            migrationBuilder.RenameColumn(
                name: "qualification_details",
                table: "staff",
                newName: "exit_reason");

            migrationBuilder.RenameColumn(
                name: "qualification",
                table: "staff",
                newName: "highest_qualification");

            migrationBuilder.RenameColumn(
                name: "is_teaching",
                table: "staff",
                newName: "statutory_is_pf_applicable");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "staff",
                newName: "statutory_is_esi_applicable");

            migrationBuilder.RenameColumn(
                name: "experience_years",
                table: "staff",
                newName: "prior_experience_years");

            migrationBuilder.AlterColumn<string>(
                name: "photo_url",
                table: "staff",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "joining_date",
                table: "staff",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alternate_phone",
                table: "staff",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_holder_name",
                table: "staff",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_number",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_branch_name",
                table: "staff",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_ifsc_code",
                table: "staff",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "staff",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "blood_group",
                table: "staff",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "confirmation_date",
                table: "staff",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "staff",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "staff",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_relation",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "marital_status",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nationality",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pan_number",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_city",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_country",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_district",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_line1",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_line2",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_postal_code",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address_state",
                table: "staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personal_email",
                table: "staff",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "probation_end_date",
                table: "staff",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "professional_registration_no",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "religion",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reporting_to_staff_id",
                table: "staff",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialization",
                table: "staff",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "staff_type",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "staff",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "statutory_esi_number",
                table: "staff",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "statutory_pf_number",
                table: "staff",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "statutory_uan",
                table: "staff",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "staff_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_documents_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_documents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    responsibilities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    leaving_reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_experiences", x => x.id);
                    table.CheckConstraint("ck_staff_experiences_dates", "to_date IS NULL OR to_date >= from_date");
                    table.ForeignKey(
                        name: "fk_staff_experiences_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_experiences_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_qualifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    degree = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    specialization = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    board_or_university = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    year_of_passing = table.Column<int>(type: "integer", nullable: true),
                    score = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_highest = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_qualifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_qualifications_staff_staff_id",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_qualifications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staff_branch_id_status_staff_type",
                table: "staff",
                columns: new[] { "branch_id", "status", "staff_type" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_reporting_to_staff_id",
                table: "staff",
                column: "reporting_to_staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_tenant_id_phone",
                table: "staff",
                columns: new[] { "tenant_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_documents_expires_on",
                table: "staff_documents",
                column: "expires_on",
                filter: "expires_on IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_staff_documents_staff_id",
                table: "staff_documents",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_documents_tenant_id",
                table: "staff_documents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_experiences_staff_id",
                table: "staff_experiences",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_experiences_tenant_id",
                table: "staff_experiences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_qualifications_staff_id",
                table: "staff_qualifications",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_qualifications_tenant_id",
                table: "staff_qualifications",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_staff_staff_reporting_to_staff_id",
                table: "staff",
                column: "reporting_to_staff_id",
                principalTable: "staff",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_staff_staff_reporting_to_staff_id",
                table: "staff");

            migrationBuilder.DropTable(
                name: "staff_documents");

            migrationBuilder.DropTable(
                name: "staff_experiences");

            migrationBuilder.DropTable(
                name: "staff_qualifications");

            migrationBuilder.DropIndex(
                name: "ix_staff_branch_id_status_staff_type",
                table: "staff");

            migrationBuilder.DropIndex(
                name: "ix_staff_reporting_to_staff_id",
                table: "staff");

            migrationBuilder.DropIndex(
                name: "ix_staff_tenant_id_phone",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "alternate_phone",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "bank_account_holder_name",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "bank_account_number",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "bank_branch_name",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "bank_ifsc_code",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "blood_group",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "category",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "confirmation_date",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "emergency_contact_name",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "emergency_contact_phone",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "emergency_contact_relation",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "marital_status",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "nationality",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "pan_number",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_city",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_country",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_district",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_line1",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_line2",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_postal_code",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "permanent_address_state",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "personal_email",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "probation_end_date",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "professional_registration_no",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "religion",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "reporting_to_staff_id",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "specialization",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "staff_type",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "status",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "statutory_esi_number",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "statutory_pf_number",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "statutory_uan",
                table: "staff");

            migrationBuilder.RenameColumn(
                name: "statutory_is_pf_applicable",
                table: "staff",
                newName: "is_teaching");

            migrationBuilder.RenameColumn(
                name: "statutory_is_esi_applicable",
                table: "staff",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "prior_experience_years",
                table: "staff",
                newName: "experience_years");

            migrationBuilder.RenameColumn(
                name: "highest_qualification",
                table: "staff",
                newName: "qualification");

            migrationBuilder.RenameColumn(
                name: "exit_reason",
                table: "staff",
                newName: "qualification_details");

            migrationBuilder.AlterColumn<string>(
                name: "photo_url",
                table: "staff",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "joining_date",
                table: "staff",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "previous_employer",
                table: "staff",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_tenant_id",
                table: "staff",
                column: "tenant_id");
        }
    }
}
