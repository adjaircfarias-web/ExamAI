# Dockerfile - PostgreSQL para ExamAI
FROM postgres:16-alpine

# Metadados
LABEL maintainer="Adjair Farias <adjaircfarias@gmail.com>"
LABEL description="PostgreSQL database for Medical Exam Extractor API"
LABEL version="1.0"

# Variáveis de ambiente padrão
ENV POSTGRES_DB=examai
ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=postgres123
ENV PGDATA=/var/lib/postgresql/data/pgdata

# Expor porta padrão
EXPOSE 5432

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD pg_isready -U postgres -d examai || exit 1

# Comando de inicialização
CMD ["postgres", "-c", "log_statement=all", "-c", "log_destination=stderr"]
