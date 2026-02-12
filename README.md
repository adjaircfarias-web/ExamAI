# 🏥 ExamAI

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white" alt="PostgreSQL 16">
  <img src="https://img.shields.io/badge/Ollama-Local-F5A623?logo=ollama&logoColor=white" alt="Ollama Local">
  <img src="https://img.shields.io/badge/Version-1.4.0-brightgreen" alt="Version 1.4.0">
  <img src="https://img.shields.io/badge/License-MIT-blue" alt="MIT License">
</p>

<p align="center">
  <strong>API for automatic extraction of medical exam data using local AI (Ollama)</strong><br>
  <strong>API para extração automática de dados de exames médicos usando IA local (Ollama)</strong>
</p>

<p align="center">
  🇺🇸 <a href="#-english">English</a> • 🇧🇷 <a href="#-português">Português</a>
</p>

---

# 🇺🇸 English

## 🚀 Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) with model `phi4:14b` or `llama3.1:8b`

### Docker Compose (Recommended)

```bash
# Start PostgreSQL + API
docker-compose up -d

# Access Swagger
# http://localhost:5076/swagger
```

### Local Development

```bash
# Copy environment variables
cp .env.example .env

# Start PostgreSQL (via Docker)
docker-compose up -d postgres

# Apply migrations and run API
cd src/ExamAI.Api
dotnet ef database update
dotnet run
```

### Ollama Setup

```bash
# Install model
ollama pull phi4:14b

# Or use smaller model (8B parameters)
ollama pull llama3.1:8b
```

---

## ✨ Features

### Complete Pipeline
- **Upload** documents (PDF, Word, Excel) - max 10MB
- **Extraction** with specialized parsers (iText7, EPPlus, OpenXml)
- **Processing** with local AI (Ollama LLM - phi4:14b or llama3.1:8b)
- **Validation** (15+ consistency rules)
- **Normalization** (30+ nomenclature mappings)
- **Persistence** in PostgreSQL with ACID

### Duplicate Detection
- SHA256 hash for all documents
- Instant return (< 100ms) for duplicates
- Saves LLM processing resources

### CPF Extraction
- AI extracts patient CPF from documents
- Automatic patient matching by CPF or name
- Nullable CPF field in database

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────┐
│  Client (Postman, cURL, Frontend)           │
└──────────────────┬──────────────────────────┘
                   │ HTTP/REST
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Api (Minimal API + Swagger)          │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Application                         │
│  ├── MedicalExamPipeline (Orchestrator)     │
│  ├── DocumentParserAgent                    │
│  ├── ExtractionAgent (Ollama)              │
│  ├── ValidationAgent                       │
│  └── NormalizationAgent                     │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Infrastructure                      │
│  ├── Parsers (PDF, Word, Excel)            │
│  ├── Repositories (EF Core)                │
│  └── Services (Hash)                       │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  PostgreSQL 16                              │
└─────────────────────────────────────────────┘
```

### Project Structure

```
ExamAI/
├── src/
│   ├── ExamAI.Api/              # REST API + Swagger
│   ├── ExamAI.Application/      # Pipeline + Agents + DTOs
│   ├── ExamAI.Domain/           # Entities + Interfaces
│   └── ExamAI.Infrastructure/   # Parsers + Repository + DbContext
├── test/
│   └── ExamAI.Tests/            # Unit tests (105 tests)
├── docker/
│   └── postgres/                # PostgreSQL Docker configs
├── docker-compose.yml           # Docker orchestration
├── .env.example                 # Environment variables
└── Makefile                     # Utility commands
```

---

## 📦 Technologies

| Category | Technology | Version |
|----------|------------|---------|
| **Framework** | .NET | 10.0 |
| **Database** | PostgreSQL | 16 |
| **ORM** | Entity Framework Core | 10 |
| **AI/LLM** | Ollama | phi4:14b / llama3.1:8b |
| **PDF Parser** | iText7 | 9.5.0 |
| **Word Parser** | DocumentFormat.OpenXml | 3.4.1 |
| **Excel Parser** | EPPlus | 8.4.1 |
| **API Docs** | Swashbuckle.AspNetCore | 10.1.1 |

---

## 🔌 API Endpoints

### Production

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/process-and-save` | Process and save exam (synchronous) |
| `GET` | `/api/exams` | List all exams with pagination and filters |
| `GET` | `/api/exams/patient/{cpf}` | Search exams by CPF |
| `DELETE` | `/api/exams/{documentId}` | Delete document |

### Health Checks

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | General health check |
| `GET` | `/health/ollama` | Ollama status |
| `GET` | `/health/database` | PostgreSQL status |
| `GET` | `/swagger` | Swagger UI documentation |

### Usage Examples

```bash
# Upload exam
curl -X POST http://localhost:5076/api/process-and-save \
  -F "file=@exam.pdf"

# List all exams (paginated, 100 max per page)
curl "http://localhost:5076/api/exams?page=1&pageSize=20"

# Search by patient name
curl "http://localhost:5076/api/exams?patientName=Silva"

# Search by CPF
curl "http://localhost:5076/api/exams/patient/12345678900"

# Delete document
curl -X DELETE http://localhost:5076/api/exams/{documentId}
```

---

## 🧪 Tests

The project includes 105 unit tests:

```bash
# Run all tests
dotnet test

# Run with details
dotnet test --verbosity normal

# Run specific tests
dotnet test --filter "FullyQualifiedName~ValidationAgent"
```

---

## 📖 Documentation

### Docker
- **[docker/README.md](docker/README.md)** - Complete Docker configuration

### Plans and Specifications
- **[Plan/Plano-Projeto-API.md](Plan/Plano-Projeto-API.md)** - Complete technical plan
- **[Plan/User-Stories.md](Plan/User-Stories.md)** - Implemented User Stories

---

## 🔧 Configuration

### Environment Variables

Copy `.env.example` to `.env` and adjust:

```bash
# PostgreSQL
POSTGRES_DB=examai
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres123
POSTGRES_PORT=5432

# Ollama
OLLAMA_URL=http://localhost:11434
OLLAMA_MODEL=phi4:14b
OLLAMA_TIMEOUT_SECONDS=300

# API
API_PORT=5076
ASPNETCORE_ENVIRONMENT=Development
```

### Default Access

| Service | URL | Credentials |
|---------|-----|-------------|
| API | http://localhost:5076 | - |
| Swagger | http://localhost:5076/swagger | - |
| PostgreSQL | localhost:5432 | postgres / postgres123 |

---

## 🗄️ Database

### Main Tables

```sql
patients            -- Patient data (with CPF)
documents           -- Uploaded files (with SHA256 hash)
exam_types          -- Exam types catalog (seeded)
exams               -- Performed exams
exam_results        -- Results for each parameter
```

### Relationships

```
patients (1) ─── (N) documents
documents (1) ─── (N) exams
exam_types (1) ─── (N) exams
exams (1) ─── (N) exam_results
```

---

## 📋 Makefile Commands

```bash
make help           # Show help
make docker-up      # Start PostgreSQL + API
make docker-down    # Stop Docker
make run            # Start API
make build          # Build project
make test           # Run tests
make clean          # Clean build artifacts
make status         # Container status
```

---

## 🎯 Project Status

| Sprint | Description | Status |
|--------|-------------|--------|
| 1 | Setup (PostgreSQL, Ollama, EF Core) | ✅ Complete |
| 2 | Parsing (PDF, Word, Excel) | ✅ Complete |
| 3 | AI Extraction (LLM + Pipeline) | ✅ Complete |
| 4 | Persistence (Database + Hash) | ✅ Complete |
| 5 | REST API (Endpoints + Swagger) | ✅ Complete |
| 6 | CPF Extraction & Patient Matching | ✅ Complete |
| **MVP** | | **✅ 100%** |

**Metrics:**
- 22 User Stories implemented
- 5 production endpoints
- ~2,500 lines of code
- 105 unit tests (88% coverage)
- Build: 0 errors, 0 warnings

---

## 📝 Changelog

### v1.4.0 (2026-02-11)
- ✅ CPF extraction from documents via AI
- ✅ Automatic patient matching by CPF (priority) or name
- ✅ New endpoint: `GET /api/exams` with pagination and filters
- ✅ Removed: pgAdmin (Docker compose)
- ✅ Removed: `/api/exams/reprocess/{documentId}` endpoint
- ✅ Ollama timeout increased to 300 seconds
- ✅ Improved PDF parsing with multiple strategies
- ✅ Database: nullable CPF field for patients

### v1.3.0 (2026-02-04)
- ✅ Simplified API: removed `cpf` and `nomePaciente` parameters from upload
- ✅ Auto-extraction: Patient data fully extracted from document by AI
- ✅ Graceful defaults for unidentified patients
- ✅ Production focus: essential endpoints only

### v1.0.0 (2026-02-03)
- ✅ Initial MVP release
- ✅ Complete end-to-end processing pipeline
- ✅ Production-ready REST API

---

## 👤 Author

**Adjair Farias**
- LinkedIn: [linkedin.com/in/farias-dev](https://linkedin.com/in/farias-dev)
- Email: adjaircfarias@gmail.com
- GitHub: [github.com/adjaircfarias](https://github.com/adjaircfarias)

---

## 📄 License

This project is licensed under the MIT License.

---

<p align="center">
  <strong>🎊 MVP 100% COMPLETE AND FUNCTIONAL! 🎊</strong>
</p>

<p align="center">
  Last update: February 2026 • Version 1.4.0 • Production Ready
</p>

---

# 🇧🇷 Português

## 🚀 Quick Start

### Pré-requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) com modelo `phi4:14b` ou `llama3.1:8b`

### Docker Compose (Recomendado)

```bash
# Iniciar PostgreSQL + API
docker-compose up -d

# Acessar Swagger
# http://localhost:5076/swagger
```

### Desenvolvimento Local

```bash
# Copiar variáveis de ambiente
cp .env.example .env

# Iniciar PostgreSQL (via Docker)
docker-compose up -d postgres

# Aplicar migrations e rodar API
cd src/ExamAI.Api
dotnet ef database update
dotnet run
```

### Configuração do Ollama

```bash
# Instalar modelo
ollama pull phi4:14b

# Ou usar modelo menor (8B parâmetros)
ollama pull llama3.1:8b
```

---

## ✨ Funcionalidades

### Pipeline Completo
- **Upload** de documentos (PDF, Word, Excel) - máximo 10MB
- **Extração** de texto com parsers especializados (iText7, EPPlus, OpenXml)
- **Processamento** com IA local (Ollama LLM - phi4:14b ou llama3.1:8b)
- **Validação** de dados (15+ regras de consistência)
- **Normalização** de nomenclatura (30+ mapeamentos)
- **Persistência** em PostgreSQL com ACID

### Detecção de Duplicatas
- Hash SHA256 para todos os documentos
- Retorno instantâneo (< 100ms) para duplicatas
- Economia de recursos de LLM

### Extração de CPF
- IA extrai CPF do paciente dos documentos
- Busca automática de paciente por CPF ou nome
- Campo CPF nullable no banco de dados

---

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────┐
│  Cliente (Postman, cURL, Frontend)          │
└──────────────────┬──────────────────────────┘
                   │ HTTP/REST
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Api (Minimal API + Swagger)         │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Application                         │
│  ├── MedicalExamPipeline (Orquestrador)     │
│  ├── DocumentParserAgent                    │
│  ├── ExtractionAgent (Ollama)                │
│  ├── ValidationAgent                        │
│  └── NormalizationAgent                     │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Infrastructure                      │
│  ├── Parsers (PDF, Word, Excel)            │
│  ├── Repositories (EF Core)                 │
│  └── Services (Hash)                        │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  PostgreSQL 16                              │
└─────────────────────────────────────────────┘
```

### Estrutura do Projeto

```
ExamAI/
├── src/
│   ├── ExamAI.Api/              # REST API + Swagger
│   ├── ExamAI.Application/      # Pipeline + Agents + DTOs
│   ├── ExamAI.Domain/           # Entidades + Interfaces
│   └── ExamAI.Infrastructure/   # Parsers + Repository + DbContext
├── test/
│   └── ExamAI.Tests/            # Testes unitários (105 testes)
├── docker/
│   └── postgres/                # Configurações Docker PostgreSQL
├── docker-compose.yml           # Orquestração Docker
├── .env.example                 # Variáveis de ambiente
└── Makefile                     # Comandos utilitários
```

---

## 📦 Tecnologias

| Categoria | Tecnologia | Versão |
|-----------|------------|--------|
| **Framework** | .NET | 10.0 |
| **Banco de Dados** | PostgreSQL | 16 |
| **ORM** | Entity Framework Core | 10 |
| **IA/LLM** | Ollama | phi4:14b / llama3.1:8b |
| **PDF Parser** | iText7 | 9.5.0 |
| **Word Parser** | DocumentFormat.OpenXml | 3.4.1 |
| **Excel Parser** | EPPlus | 8.4.1 |
| **API Docs** | Swashbuckle.AspNetCore | 10.1.1 |

---

## 🔌 API Endpoints

### Produção

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/process-and-save` | Processar e salvar exame (síncrono) |
| `GET` | `/api/exams` | Listar todos os exames com paginação e filtros |
| `GET` | `/api/exams/patient/{cpf}` | Buscar exames por CPF |
| `DELETE` | `/api/exams/{documentId}` | Deletar documento |

### Health Checks

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/health` | Health check geral |
| `GET` | `/health/ollama` | Status do Ollama |
| `GET` | `/health/database` | Status do PostgreSQL |
| `GET` | `/swagger` | Documentação Swagger UI |

### Exemplo de Uso

```bash
# Upload de exame
curl -X POST http://localhost:5076/api/process-and-save \
  -F "file=@exame.pdf"

# Listar todos os exames (paginado, 100 máx por página)
curl "http://localhost:5076/api/exams?page=1&pageSize=20"

# Buscar por nome do paciente
curl "http://localhost:5076/api/exams?patientName=Silva"

# Buscar por CPF
curl "http://localhost:5076/api/exams/patient/12345678900"

# Deletar documento
curl -X DELETE http://localhost:5076/api/exams/{documentId}
```

---

## 🧪 Testes

O projeto inclui 105 testes unitários:

```bash
# Executar todos os testes
dotnet test

# Executar com detalhes
dotnet test --verbosity normal

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~ValidationAgent"
```

---

## 📖 Documentação

### Docker
- **[docker/README.md](docker/README.md)** - Configuração Docker completa

### Planos e Especificações
- **[Plan/Plano-Projeto-API.md](Plan/Plano-Projeto-API.md)** - Plano técnico completo
- **[Plan/User-Stories.md](Plan/User-Stories.md)** - User Stories implementadas

---

## 🔧 Configuração

### Variáveis de Ambiente

Copie `.env.example` para `.env` e ajuste:

```bash
# PostgreSQL
POSTGRES_DB=examai
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres123
POSTGRES_PORT=5432

# Ollama
OLLAMA_URL=http://localhost:11434
OLLAMA_MODEL=phi4:14b
OLLAMA_TIMEOUT_SECONDS=300

# API
API_PORT=5076
ASPNETCORE_ENVIRONMENT=Development
```

### Acessos Padrão

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| API | http://localhost:5076 | - |
| Swagger | http://localhost:5076/swagger | - |
| PostgreSQL | localhost:5432 | postgres / postgres123 |

---

## 🗄️ Banco de Dados

### Tabelas Principais

```sql
patients            -- Dados do paciente (com CPF)
documents           -- Arquivos uploadados (com hash SHA256)
exam_types          -- Catálogo de tipos de exame (seeded)
exams               -- Exames realizados
exam_results        -- Resultados de cada parâmetro
```

### Relacionamentos

```
patients (1) ─── (N) documents
documents (1) ─── (N) exams
exam_types (1) ─── (N) exams
exams (1) ─── (N) exam_results
```

---

## 📋 Comandos Makefile

```bash
make help           # Mostrar ajuda
make docker-up      # Iniciar PostgreSQL + API
make docker-down    # Parar Docker
make run            # Iniciar API
make build          # Build do projeto
make test           # Executar testes
make clean          # Limpar build artifacts
make status         # Status dos containers
```

---

## 🎯 Status do Projeto

| Sprint | Descrição | Status |
|--------|-----------|--------|
| 1 | Setup (PostgreSQL, Ollama, EF Core) | ✅ Completo |
| 2 | Parsing (PDF, Word, Excel) | ✅ Completo |
| 3 | AI Extraction (LLM + Pipeline) | ✅ Completo |
| 4 | Persistence (Database + Hash) | ✅ Completo |
| 5 | REST API (Endpoints + Swagger) | ✅ Completo |
| 6 | Extração de CPF e Busca de Paciente | ✅ Completo |
| **MVP** | | **✅ 100%** |

**Métricas:**
- 22 User Stories implementadas
- 5 endpoints de produção
- ~2.500 linhas de código
- 105 testes unitários (88% cobertura)
- Build: 0 erros, 0 warnings

---

## 📝 Changelog

### v1.4.0 (2026-02-11)
- ✅ Extração de CPF dos documentos via IA
- ✅ Busca automática de pacientes por CPF (prioridade) ou nome
- ✅ Novo endpoint: `GET /api/exams` com paginação e filtros
- ✅ Removido: pgAdmin (Docker compose)
- ✅ Removido: endpoint `/api/exams/reprocess/{documentId}`
- ✅ Timeout do Ollama aumentado para 300 segundos
- ✅ Melhorado parsing de PDF com múltiplas estratégias
- ✅ Banco: campo CPF nullable para pacientes

### v1.3.0 (2026-02-04)
- ✅ API simplificada: removidos parâmetros `cpf` e `nomePaciente` do upload
- ✅ Extração automática de dados do paciente via IA
- ✅ Tratamento graceful para pacientes não identificados
- ✅ Foco em produção: apenas endpoints essenciais

### v1.0.0 (2026-02-03)
- ✅ Lançamento inicial MVP
- ✅ Pipeline completo end-to-end
- ✅ API REST pronta para produção

---

## 👤 Autor

**Adjair Farias**
- LinkedIn: [linkedin.com/in/farias-dev](https://linkedin.com/in/farias-dev)
- Email: adjaircfarias@gmail.com
- GitHub: [github.com/adjaircfarias](https://github.com/adjaircfarias)

---

## 📄 Licença

Este projeto está licenciado sob a Licença MIT.

---

<p align="center">
  <strong>🎊 MVP 100% COMPLETO E FUNCIONAL! 🎊</strong>
</p>

<p align="center">
  Última atualização: Fevereiro 2026 • Versão 1.4.0 • Pronto para Produção
</p>
