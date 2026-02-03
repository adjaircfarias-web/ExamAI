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
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_nascimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_exame",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_exame", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tipo_arquivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    data_upload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status_processamento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    erro_processamento = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_documentos_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exames",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_exame_id = table.Column<int>(type: "integer", nullable: true),
                    data_coleta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    medico_solicitante = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    laboratorio = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exames", x => x.id);
                    table.ForeignKey(
                        name: "FK_exames_documentos_documento_id",
                        column: x => x.documento_id,
                        principalTable: "documentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exames_tipos_exame_tipo_exame_id",
                        column: x => x.tipo_exame_id,
                        principalTable: "tipos_exame",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "resultados_exame",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exame_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parametro = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    valor_numerico = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    valor_texto = table.Column<string>(type: "text", nullable: true),
                    unidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    referencia_min = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    referencia_max = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultados_exame", x => x.id);
                    table.ForeignKey(
                        name: "FK_resultados_exame_exames_exame_id",
                        column: x => x.exame_id,
                        principalTable: "exames",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tipos_exame",
                columns: new[] { "id", "categoria", "created_at", "descricao", "nome" },
                values: new object[,]
                {
                    { 1, "Hematologia", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(5723), null, "Hemograma Completo" },
                    { 2, "Bioquímica", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6135), null, "Glicemia" },
                    { 3, "Lipidograma", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6138), null, "Colesterol Total" },
                    { 4, "Lipidograma", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6139), null, "HDL" },
                    { 5, "Lipidograma", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6140), null, "LDL" },
                    { 6, "Lipidograma", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6141), null, "Triglicerídeos" },
                    { 7, "Função Renal", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6143), null, "Ureia" },
                    { 8, "Função Renal", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6144), null, "Creatinina" },
                    { 9, "Função Hepática", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6145), null, "TGO/AST" },
                    { 10, "Função Hepática", new DateTime(2026, 2, 3, 1, 27, 24, 856, DateTimeKind.Utc).AddTicks(6146), null, "TGP/ALT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_documentos_hash_sha256",
                table: "documentos",
                column: "hash_sha256");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_paciente_id",
                table: "documentos",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_status_processamento",
                table: "documentos",
                column: "status_processamento");

            migrationBuilder.CreateIndex(
                name: "IX_exames_data_coleta",
                table: "exames",
                column: "data_coleta");

            migrationBuilder.CreateIndex(
                name: "IX_exames_documento_id",
                table: "exames",
                column: "documento_id");

            migrationBuilder.CreateIndex(
                name: "IX_exames_tipo_exame_id",
                table: "exames",
                column: "tipo_exame_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_cpf",
                table: "pacientes",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_nome",
                table: "pacientes",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_exame_exame_id",
                table: "resultados_exame",
                column: "exame_id");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_exame_parametro",
                table: "resultados_exame",
                column: "parametro");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_exame_status",
                table: "resultados_exame",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_exame_categoria",
                table: "tipos_exame",
                column: "categoria");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_exame_nome",
                table: "tipos_exame",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resultados_exame");

            migrationBuilder.DropTable(
                name: "exames");

            migrationBuilder.DropTable(
                name: "documentos");

            migrationBuilder.DropTable(
                name: "tipos_exame");

            migrationBuilder.DropTable(
                name: "pacientes");
        }
    }
}
