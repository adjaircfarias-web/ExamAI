using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExamAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exam_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    processing_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    processing_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_documents_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_type_id = table.Column<int>(type: "integer", nullable: true),
                    collection_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requesting_physician = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    laboratory = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exams", x => x.id);
                    table.ForeignKey(
                        name: "FK_exams_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exams_exam_types_exam_type_id",
                        column: x => x.exam_type_id,
                        principalTable: "exam_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "exam_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parameter = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    numeric_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    text_value = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_min = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    reference_max = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_results_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "exam_types",
                columns: new[] { "id", "category", "created_at", "description", "name" },
                values: new object[,]
                {
                    { 1, "Hematologia", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Hemograma Completo" },
                    { 2, "Bioquímica", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Glicemia" },
                    { 3, "Lipidograma", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Colesterol Total" },
                    { 4, "Lipidograma", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HDL" },
                    { 5, "Lipidograma", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "LDL" },
                    { 6, "Lipidograma", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Triglicerídeos" },
                    { 7, "Função Renal", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ureia" },
                    { 8, "Função Renal", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Creatinina" },
                    { 9, "Função Hepática", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TGO/AST" },
                    { 10, "Função Hepática", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TGP/ALT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_hash_sha256",
                table: "documents",
                column: "hash_sha256");

            migrationBuilder.CreateIndex(
                name: "IX_documents_patient_id",
                table: "documents",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_processing_status",
                table: "documents",
                column: "processing_status");

            migrationBuilder.CreateIndex(
                name: "IX_exam_results_exam_id",
                table: "exam_results",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_results_parameter",
                table: "exam_results",
                column: "parameter");

            migrationBuilder.CreateIndex(
                name: "IX_exam_results_status",
                table: "exam_results",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_exam_types_category",
                table: "exam_types",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_exam_types_name",
                table: "exam_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exams_collection_date",
                table: "exams",
                column: "collection_date");

            migrationBuilder.CreateIndex(
                name: "IX_exams_document_id",
                table: "exams",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_exams_exam_type_id",
                table: "exams",
                column: "exam_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_cpf",
                table: "patients",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patients_name",
                table: "patients",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_results");

            migrationBuilder.DropTable(
                name: "exams");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "exam_types");

            migrationBuilder.DropTable(
                name: "patients");
        }
    }
}
