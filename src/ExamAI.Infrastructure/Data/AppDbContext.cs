using ExamAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamAI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<ExamType> ExamTypes { get; set; } = null!;
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<ExamResult> ExamResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de Patient
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Cpf).HasColumnName("cpf").HasMaxLength(11);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Cpf).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // Configuração de Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.HashSha256).HasColumnName("hash_sha256").HasMaxLength(64).IsRequired();
            entity.Property(e => e.UploadDate).HasColumnName("upload_date").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(50).HasDefaultValue("pending");
            entity.Property(e => e.ProcessingError).HasColumnName("processing_error");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Documents)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.HashSha256);
            entity.HasIndex(e => e.ProcessingStatus);
        });

        // Configuração de ExamType
        modelBuilder.Entity<ExamType>(entity =>
        {
            entity.ToTable("exam_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);

            // Seed data - Usar data fixa para evitar problemas com migrations
            var seedDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            entity.HasData(
                new ExamType { Id = 1, Name = "Hemograma Completo", Category = "Hematologia", CreatedAt = seedDate },
                new ExamType { Id = 2, Name = "Glicemia", Category = "Bioquímica", CreatedAt = seedDate },
                new ExamType { Id = 3, Name = "Colesterol Total", Category = "Lipidograma", CreatedAt = seedDate },
                new ExamType { Id = 4, Name = "HDL", Category = "Lipidograma", CreatedAt = seedDate },
                new ExamType { Id = 5, Name = "LDL", Category = "Lipidograma", CreatedAt = seedDate },
                new ExamType { Id = 6, Name = "Triglicerídeos", Category = "Lipidograma", CreatedAt = seedDate },
                new ExamType { Id = 7, Name = "Ureia", Category = "Função Renal", CreatedAt = seedDate },
                new ExamType { Id = 8, Name = "Creatinina", Category = "Função Renal", CreatedAt = seedDate },
                new ExamType { Id = 9, Name = "TGO/AST", Category = "Função Hepática", CreatedAt = seedDate },
                new ExamType { Id = 10, Name = "TGP/ALT", Category = "Função Hepática", CreatedAt = seedDate }
            );
        });

        // Configuração de Exam
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("exams");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.ExamTypeId).HasColumnName("exam_type_id");
            entity.Property(e => e.CollectionDate).HasColumnName("collection_date");
            entity.Property(e => e.RequestingPhysician).HasColumnName("requesting_physician").HasMaxLength(255);
            entity.Property(e => e.Laboratory).HasColumnName("laboratory").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Document)
                .WithMany(d => d.Exams)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ExamType)
                .WithMany(t => t.Exams)
                .HasForeignKey(e => e.ExamTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.ExamTypeId);
            entity.HasIndex(e => e.CollectionDate);
        });

        // Configuração de ExamResult
        modelBuilder.Entity<ExamResult>(entity =>
        {
            entity.ToTable("exam_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.Parameter).HasColumnName("parameter").HasMaxLength(255).IsRequired();
            entity.Property(e => e.NumericValue).HasColumnName("numeric_value").HasPrecision(18, 4);
            entity.Property(e => e.TextValue).HasColumnName("text_value");
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(50);
            entity.Property(e => e.ReferenceMin).HasColumnName("reference_min").HasPrecision(18, 4);
            entity.Property(e => e.ReferenceMax).HasColumnName("reference_max").HasPrecision(18, 4);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.Observations).HasColumnName("observations");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Exam)
                .WithMany(ex => ex.Results)
                .HasForeignKey(e => e.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ExamId);
            entity.HasIndex(e => e.Parameter);
            entity.HasIndex(e => e.Status);
        });
    }
}
