using ExamAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamAI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Paciente> Pacientes { get; set; } = null!;
    public DbSet<Documento> Documentos { get; set; } = null!;
    public DbSet<TipoExame> TiposExame { get; set; } = null!;
    public DbSet<Exame> Exames { get; set; } = null!;
    public DbSet<ResultadoExame> ResultadosExame { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de Paciente
        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.ToTable("pacientes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
            entity.Property(e => e.DataNascimento).HasColumnName("data_nascimento");
            entity.Property(e => e.Cpf).HasColumnName("cpf").HasMaxLength(11);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Cpf).IsUnique();
            entity.HasIndex(e => e.Nome);
        });

        // Configuração de Documento
        modelBuilder.Entity<Documento>(entity =>
        {
            entity.ToTable("documentos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PacienteId).HasColumnName("paciente_id");
            entity.Property(e => e.NomeArquivo).HasColumnName("nome_arquivo").HasMaxLength(500).IsRequired();
            entity.Property(e => e.TipoArquivo).HasColumnName("tipo_arquivo").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TamanhoBytes).HasColumnName("tamanho_bytes");
            entity.Property(e => e.HashSha256).HasColumnName("hash_sha256").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DataUpload).HasColumnName("data_upload").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.StatusProcessamento).HasColumnName("status_processamento").HasMaxLength(50).HasDefaultValue("pending");
            entity.Property(e => e.ErroProcessamento).HasColumnName("erro_processamento");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Paciente)
                .WithMany(p => p.Documentos)
                .HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PacienteId);
            entity.HasIndex(e => e.HashSha256);
            entity.HasIndex(e => e.StatusProcessamento);
        });

        // Configuração de TipoExame
        modelBuilder.Entity<TipoExame>(entity =>
        {
            entity.ToTable("tipos_exame");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Descricao).HasColumnName("descricao");
            entity.Property(e => e.Categoria).HasColumnName("categoria").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Nome).IsUnique();
            entity.HasIndex(e => e.Categoria);

            // Seed data - Usar data fixa para evitar problemas com migrations
            var seedDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            entity.HasData(
                new TipoExame { Id = 1, Nome = "Hemograma Completo", Categoria = "Hematologia", CreatedAt = seedDate },
                new TipoExame { Id = 2, Nome = "Glicemia", Categoria = "Bioquímica", CreatedAt = seedDate },
                new TipoExame { Id = 3, Nome = "Colesterol Total", Categoria = "Lipidograma", CreatedAt = seedDate },
                new TipoExame { Id = 4, Nome = "HDL", Categoria = "Lipidograma", CreatedAt = seedDate },
                new TipoExame { Id = 5, Nome = "LDL", Categoria = "Lipidograma", CreatedAt = seedDate },
                new TipoExame { Id = 6, Nome = "Triglicerídeos", Categoria = "Lipidograma", CreatedAt = seedDate },
                new TipoExame { Id = 7, Nome = "Ureia", Categoria = "Função Renal", CreatedAt = seedDate },
                new TipoExame { Id = 8, Nome = "Creatinina", Categoria = "Função Renal", CreatedAt = seedDate },
                new TipoExame { Id = 9, Nome = "TGO/AST", Categoria = "Função Hepática", CreatedAt = seedDate },
                new TipoExame { Id = 10, Nome = "TGP/ALT", Categoria = "Função Hepática", CreatedAt = seedDate }
            );
        });

        // Configuração de Exame
        modelBuilder.Entity<Exame>(entity =>
        {
            entity.ToTable("exames");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocumentoId).HasColumnName("documento_id");
            entity.Property(e => e.TipoExameId).HasColumnName("tipo_exame_id");
            entity.Property(e => e.DataColeta).HasColumnName("data_coleta");
            entity.Property(e => e.MedicoSolicitante).HasColumnName("medico_solicitante").HasMaxLength(255);
            entity.Property(e => e.Laboratorio).HasColumnName("laboratorio").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Documento)
                .WithMany(d => d.Exames)
                .HasForeignKey(e => e.DocumentoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TipoExame)
                .WithMany(t => t.Exames)
                .HasForeignKey(e => e.TipoExameId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DocumentoId);
            entity.HasIndex(e => e.TipoExameId);
            entity.HasIndex(e => e.DataColeta);
        });

        // Configuração de ResultadoExame
        modelBuilder.Entity<ResultadoExame>(entity =>
        {
            entity.ToTable("resultados_exame");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExameId).HasColumnName("exame_id");
            entity.Property(e => e.Parametro).HasColumnName("parametro").HasMaxLength(255).IsRequired();
            entity.Property(e => e.ValorNumerico).HasColumnName("valor_numerico").HasPrecision(18, 4);
            entity.Property(e => e.ValorTexto).HasColumnName("valor_texto");
            entity.Property(e => e.Unidade).HasColumnName("unidade").HasMaxLength(50);
            entity.Property(e => e.ReferenciaMin).HasColumnName("referencia_min").HasPrecision(18, 4);
            entity.Property(e => e.ReferenciaMax).HasColumnName("referencia_max").HasPrecision(18, 4);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.Observacoes).HasColumnName("observacoes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Exame)
                .WithMany(ex => ex.Resultados)
                .HasForeignKey(e => e.ExameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ExameId);
            entity.HasIndex(e => e.Parametro);
            entity.HasIndex(e => e.Status);
        });
    }
}
