using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRegistrationV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "region",
                schema: "core",
                table: "registrations");

            migrationBuilder.RenameColumn(
                name: "TentId",
                schema: "core",
                table: "registrations",
                newName: "tent_id");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                schema: "core",
                table: "registrations",
                newName: "team_id");

            migrationBuilder.RenameColumn(
                name: "participation_category",
                schema: "core",
                table: "registrations",
                newName: "state");

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyBody",
                schema: "core",
                table: "retreats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyTitle",
                schema: "core",
                table: "retreats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyVersion",
                schema: "core",
                table: "retreats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alcohol_use",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "allergies_details",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_ip",
                schema: "core",
                table: "registrations",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "drug_use_frequency",
                schema: "core",
                table: "registrations",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook_username",
                schema: "core",
                table: "registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_loss_details",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "father_name",
                schema: "core",
                table: "registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "father_phone",
                schema: "core",
                table: "registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "father_status",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "had_family_loss_last6m",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_allergies",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_medical_restriction",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_relative_or_friend_submitted",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "height_cm",
                schema: "core",
                table: "registrations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "id_document_content_type",
                schema: "core",
                table: "registrations",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_document_number",
                schema: "core",
                table: "registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_document_size_bytes",
                schema: "core",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_document_storage_key",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_document_type",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "id_document_uploaded_at",
                schema: "core",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_document_url",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instagram_handle",
                schema: "core",
                table: "registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "marital_status",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "marketing_opt_in",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "marketing_opt_in_at",
                schema: "core",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "medical_restriction_details",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medications_details",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mother_name",
                schema: "core",
                table: "registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mother_phone",
                schema: "core",
                table: "registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mother_status",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "neighbor_phone",
                schema: "core",
                table: "registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "neighborhood",
                schema: "core",
                table: "registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                schema: "core",
                table: "registrations",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_size_bytes",
                schema: "core",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_storage_key",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_uploaded_at",
                schema: "core",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "physical_limitation_details",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pregnancy",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "prev_uncalled_applications",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "profession",
                schema: "core",
                table: "registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rahamin_vida_completed",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "recent_surgery_or_procedure_details",
                schema: "core",
                table: "registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "relative_phone",
                schema: "core",
                table: "registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "religion",
                schema: "core",
                table: "registrations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shirt_size",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "smoker",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "street_and_number",
                schema: "core",
                table: "registrations",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "submitter_names",
                schema: "core",
                table: "registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "submitter_relationship",
                schema: "core",
                table: "registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<bool>(
                name: "takes_medication",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "terms_accepted",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "terms_accepted_at",
                schema: "core",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "terms_version",
                schema: "core",
                table: "registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                schema: "core",
                table: "registrations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "uses_drugs",
                schema: "core",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_kg",
                schema: "core",
                table: "registrations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp",
                schema: "core",
                table: "registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyPolicyBody",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyTitle",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyVersion",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "alcohol_use",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "allergies_details",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "client_ip",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "drug_use_frequency",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "facebook_username",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "family_loss_details",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "father_name",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "father_phone",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "father_status",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "had_family_loss_last6m",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "has_allergies",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "has_medical_restriction",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "has_relative_or_friend_submitted",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "height_cm",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_content_type",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_number",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_size_bytes",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_storage_key",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_type",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_uploaded_at",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "id_document_url",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "instagram_handle",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "marital_status",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "marketing_opt_in",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "marketing_opt_in_at",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "medical_restriction_details",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "medications_details",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "mother_name",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "mother_phone",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "mother_status",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "neighbor_phone",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "neighborhood",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "photo_content_type",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "photo_size_bytes",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "photo_storage_key",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "photo_uploaded_at",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "physical_limitation_details",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "pregnancy",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "prev_uncalled_applications",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "profession",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "rahamin_vida_completed",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "recent_surgery_or_procedure_details",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "relative_phone",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "religion",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "shirt_size",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "smoker",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "street_and_number",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "submitter_names",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "submitter_relationship",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "takes_medication",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "terms_accepted",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "terms_accepted_at",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "terms_version",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "user_agent",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "uses_drugs",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "weight_kg",
                schema: "core",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "whatsapp",
                schema: "core",
                table: "registrations");

            migrationBuilder.RenameColumn(
                name: "tent_id",
                schema: "core",
                table: "registrations",
                newName: "TentId");

            migrationBuilder.RenameColumn(
                name: "team_id",
                schema: "core",
                table: "registrations",
                newName: "TeamId");

            migrationBuilder.RenameColumn(
                name: "state",
                schema: "core",
                table: "registrations",
                newName: "participation_category");

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "core",
                table: "registrations",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
