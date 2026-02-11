# Dockerfile - ExamAI API
# Multi-stage build for .NET 10 Web API

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (for better layer caching)
COPY ExamAI.slnx ./
COPY src/ExamAI.Api/ExamAI.Api.csproj src/ExamAI.Api/
COPY src/ExamAI.Application/ExamAI.Application.csproj src/ExamAI.Application/
COPY src/ExamAI.Domain/ExamAI.Domain.csproj src/ExamAI.Domain/
COPY src/ExamAI.Infrastructure/ExamAI.Infrastructure.csproj src/ExamAI.Infrastructure/

# Restore dependencies
RUN dotnet restore src/ExamAI.Api/ExamAI.Api.csproj

# Copy all source code
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/ExamAI.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -s /bin/bash appuser && chown -R appuser /app
USER appuser

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose API port
EXPOSE 5076

# Environment variables
ENV ASPNETCORE_URLS=http://+:5076
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:5076/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "ExamAI.Api.dll"]
