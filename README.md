# 🏥 ExamAI

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white" alt="PostgreSQL 16">
  <img src="https://img.shields.io/badge/Ollama-Local-F5A623?logo=ollama&logoColor=white" alt="Ollama Local">
  <img src="https://img.shields.io/badge/Version-1.3.0-brightgreen" alt="Version 1.3.0">
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
- [Ollama](https://ollama.com) with model `llama3.1:70b`

### Option 1: Docker Compose (Recommended)

```bash
# 1. Start PostgreSQL + pgAdmin
docker-compose up -d

# 2. Apply migrations
cd src/ExamAI.Api
dotnet ef database update

# 3. Start API
dotnet run

# 4. Access Swagger
# http://localhost:5076/swagger
```

### Option 2: Using Makefile

```bash
make setup  # Complete setup
make run    # Start API
```

### Configuration

Copy the environment variables example file:

```bash
cp .env.example .env
```

---

## ✨ Features

### Complete Pipeline
- **Upload** documents (PDF, Word, Excel) - max 10MB
- **Extraction** with specialized parsers
- **Processing** with local AI (Ollama LLM)
- **Validation** (15+ consistency rules)
- **Normalization** (30+ nomenclature mappings)
- **Persistence** in PostgreSQL with ACID

### Duplicate Detection
- SHA256 hash for all documents
- Instant return (< 100ms) for duplicates
- Saves LLM processing resources

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────┐
│  Client (Postman, cURL, Frontend)           │
└──────────────────┬──────────────────────────┘
                   │ HTTP/REST
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Api (Controllers + Swagger)         │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Application                         │
│  ├── MedicalExamPipeline (Orchestrator)     │
│  ├── DocumentParserAgent                    │
│  ├── ExtractionAgent (Ollama)               │
│  ├── ValidationAgent                        │
│  └── NormalizationAgent                     │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Infrastructure                      │
│  ├── Parsers (PDF, Word, Excel)             │
│  ├── Repositories (EF Core)                 │
│  └── Services                               │
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
│   ├── ExamAI.Application/      # Pipeline + Agents
│   ├── ExamAI.Domain/           # Entities + Interfaces
│   └── ExamAI.Infrastructure/   # Parsers + Repository
├── test/
│   └── ExamAI.Tests/            # Unit and integration tests
├── docker/
│   └── postgres/                # Docker PostgreSQL configurations
├── docker-compose.yml           # Docker orchestration
├── .env.example                 # Environment variables example
└── Makefile                     # Utility commands
```

---

## 📦 Technologies

| Category | Technology | Version |
|----------|------------|---------|
| **Framework** | .NET | 10.0 |
| **Database** | PostgreSQL | 16 |
| **ORM** | Entity Framework Core | 10 |
| **AI/LLM** | Ollama | llama3.1:70b |
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
| `GET` | `/api/exams/paciente/{cpf}` | Search exams by CPF |
| `POST` | `/api/exams/reprocess/{documentoId}` | Reprocess failed document |
| `DELETE` | `/api/exams/{documentoId}` | Delete document |

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

# Search exams by CPF
curl "http://localhost:5076/api/exams/paciente/12345678900"
```

---

## 🧪 Tests

The project includes unit and integration tests:

```bash
# Run all tests
dotnet test

# Run with details
dotnet test --verbosity normal

# Run specific tests
dotnet test --filter "FullyQualifiedName~Integration"
```

---

## 📖 Documentation

### Setup Guides
- **[QUICK-START.md](QUICK-START.md)** - 5-minute quick start
- **[UPLOAD-TEST.md](UPLOAD-TEST.md)** - How to test uploads
- **[DUPLICATE-FAILED-DOCS.md](DUPLICATE-FAILED-DOCS.md)** - Manage failed documents
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Troubleshooting guide

### Docker
- **[docker/README.md](docker/README.md)** - Complete Docker configuration

### Plans and Specifications
- **[Plan/Plano-Projeto-API.md](Plan/Plano-Projeto-API.md)** - Complete technical plan
- **[Plan/User-Stories.md](Plan/User-Stories.md)** - Implemented User Stories

---

## 🔧 Configuration

### Environment Variables

Copy `.env.example` to `.env` and adjust as needed:

```bash
# PostgreSQL
POSTGRES_DB=examai
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres123
POSTGRES_PORT=5432

# Ollama
OLLAMA_URL=http://localhost:11434
OLLAMA_MODEL=llama3.1:70b
OLLAMA_TIMEOUT_SECONDS=180

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
| pgAdmin | http://localhost:5050 | admin@examai.com / admin123 |

---

## 🗄️ Database

### Main Tables

```sql
pacientes           -- Patient data
documentos          -- Uploaded files (with SHA256 hash)
tipos_exame         -- Exam types catalog
exames              -- Performed exams
resultados_exame    -- Results for each parameter
```

### Relationships

```
pacientes (1) ─── (N) documentos
documentos (1) ─── (N) exames
tipos_exame (1) ─── (N) exames
exames (1) ─── (N) resultados_exame
```

---

## 📋 Makefile Commands

```bash
make help           # Show help
make docker-up      # Start PostgreSQL + pgAdmin
make docker-down    # Stop Docker
make migrate        # Apply migrations
make run            # Start API
make build          # Build project
make test           # Run tests
make clean          # Clean build artifacts
make reset          # Complete reset (⚠️ deletes data!)
make setup          # Initial complete setup
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
| **MVP** | | **✅ 100%** |

**Metrics:**
- 20 User Stories implemented
- 7 production endpoints
- ~2,500 lines of code
- Build: 0 errors, 0 warnings

---

## 📝 Changelog

### v1.3.0 (2026-02-04)
- ✅ Simplified API: removed `cpf` and `nomePaciente` parameters from `/api/process-and-save`
- ✅ Auto-extraction: Patient data is now fully extracted from the document by AI
- ✅ Graceful defaults: Unidentified patients get `nome: "Paciente não identificado"` and `cpf: null`
- ✅ Production focus: Only 7 essential endpoints for production use
- ✅ Database update: `documentos.paciente_id` is now nullable

### v1.0.0 (2026-02-03)
- ✅ Initial MVP release
- ✅ 20 User Stories implemented
- ✅ Complete end-to-end processing pipeline
- ✅ Production-ready REST API

---

## 👤 Author

**Adjair Farias**
- LinkedIn: [linkedin.com/in/farias-dev](https://linkedin.com/in/farias-dev)
- Email: adjaircfarias@gmail.com
- GitHub: [github.com/adjaircfarias](https://github.com/adjaircfarias)

Developed with: Clawdex 🔍

---

## 📄 License

This project is licensed under the MIT License.

---

<p align="center">
  <strong>🎊 MVP 100% COMPLETE AND FUNCTIONAL! 🎊</strong>
</p>

<p align="center">
  Last update: February 2026 • Version 1.3.0 • Production Ready
</p>

---

# 🇧🇷 Português

## 🚀 Quick Start

### Pré-requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) com modelo `llama3.1:70b`

### Opção 1: Docker Compose (Recomendado)

```bash
# 1. Iniciar PostgreSQL + pgAdmin
docker-compose up -d

# 2. Aplicar migrations
cd src/ExamAI.Api
dotnet ef database update

# 3. Iniciar API
dotnet run

# 4. Acessar Swagger
# http://localhost:5076/swagger
```

### Opção 2: Usando Makefile

```bash
make setup  # Setup completo
make run    # Iniciar API
```

### Configuração

Copie o arquivo de exemplo de variáveis de ambiente:

```bash
cp .env.example .env
```

---

## ✨ Funcionalidades

### Pipeline Completo
- **Upload** de documentos (PDF, Word, Excel) - máximo 10MB
- **Extração** de texto com parsers especializados
- **Processamento** com IA local (Ollama LLM)
- **Validação** de dados (15+ regras de consistência)
- **Normalização** de nomenclatura (30+ mapeamentos)
- **Persistência** em PostgreSQL com ACID

### Detecção de Duplicatas
- Hash SHA256 para todos os documentos
- Retorno instantâneo (< 100ms) para duplicatas
- Economia de recursos de LLM

---

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────┐
│  Cliente (Postman, cURL, Frontend)          │
└──────────────────┬──────────────────────────┘
                   │ HTTP/REST
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Api (Controllers + Swagger)         │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Application                         │
│  ├── MedicalExamPipeline (Orquestrador)     │
│  ├── DocumentParserAgent                    │
│  ├── ExtractionAgent (Ollama)               │
│  ├── ValidationAgent                        │
│  └── NormalizationAgent                     │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  ExamAI.Infrastructure                      │
│  ├── Parsers (PDF, Word, Excel)             │
│  ├── Repositories (EF Core)                 │
│  └── Services                               │
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
│   ├── ExamAI.Application/      # Pipeline + Agents
│   ├── ExamAI.Domain/           # Entidades + Interfaces
│   └── ExamAI.Infrastructure/   # Parsers + Repository
├── test/
│   └── ExamAI.Tests/            # Testes unitários e integração
├── docker/
│   └── postgres/                # Configurações Docker PostgreSQL
├── docker-compose.yml           # Orquestração Docker
├── .env.example                 # Variáveis de ambiente exemplo
└── Makefile                     # Comandos utilitários
```

---

## 📦 Tecnologias

| Categoria | Tecnologia | Versão |
|-----------|------------|--------|
| **Framework** | .NET | 10.0 |
| **Banco de Dados** | PostgreSQL | 16 |
| **ORM** | Entity Framework Core | 10 |
| **IA/LLM** | Ollama | llama3.1:70b |
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
| `GET` | `/api/exams/paciente/{cpf}` | Buscar exames por CPF |
| `POST` | `/api/exams/reprocess/{documentoId}` | Reprocessar documento falho |
| `DELETE` | `/api/exams/{documentoId}` | Deletar documento |

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

# Buscar exames por CPF
curl "http://localhost:5076/api/exams/paciente/12345678900"
```

---

## 🧪 Testes

O projeto inclui testes unitários e de integração:

```bash
# Executar todos os testes
dotnet test

# Executar com detalhes
dotnet test --verbosity normal

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~Integration"
```

---

## 📖 Documentação

### Guias de Setup
- **[QUICK-START.md](QUICK-START.md)** - Guia rápido de 5 minutos
- **[UPLOAD-TEST.md](UPLOAD-TEST.md)** - Como testar uploads
- **[DUPLICATE-FAILED-DOCS.md](DUPLICATE-FAILED-DOCS.md)** - Gerenciar documentos falhos
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Solução de problemas

### Docker
- **[docker/README.md](docker/README.md)** - Configuração Docker completa

### Planos e Especificações
- **[Plan/Plano-Projeto-API.md](Plan/Plano-Projeto-API.md)** - Plano técnico completo
- **[Plan/User-Stories.md](Plan/User-Stories.md)** - User Stories implementadas

---

## 🔧 Configuração

### Variáveis de Ambiente

Copie `.env.example` para `.env` e ajuste conforme necessário:

```bash
# PostgreSQL
POSTGRES_DB=examai
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres123
POSTGRES_PORT=5432

# Ollama
OLLAMA_URL=http://localhost:11434
OLLAMA_MODEL=llama3.1:70b
OLLAMA_TIMEOUT_SECONDS=180

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
| pgAdmin | http://localhost:5050 | admin@examai.com / admin123 |

---

## 🗄️ Banco de Dados

### Tabelas Principais

```sql
pacientes           -- Dados do paciente
documentos          -- Arquivos uploadados (com hash SHA256)
tipos_exame         -- Catálogo de tipos de exame
exames              -- Exames realizados
resultados_exame    -- Resultados de cada parâmetro
```

### Relacionamentos

```
pacientes (1) ─── (N) documentos
documentos (1) ─── (N) exames
tipos_exame (1) ─── (N) exames
exames (1) ─── (N) resultados_exame
```

---

## 📋 Comandos Makefile

```bash
make help           # Mostrar ajuda
make docker-up      # Iniciar PostgreSQL + pgAdmin
make docker-down    # Parar Docker
make migrate        # Aplicar migrations
make run            # Iniciar API
make build          # Build do projeto
make test           # Executar testes
make clean          # Limpar build artifacts
make reset          # Reset completo (⚠️ apaga dados!)
make setup          # Setup inicial completo
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
| **MVP** | | **✅ 100%** |

**Métricas:**
- 20 User Stories implementadas
- 7 endpoints de produção
- ~2.500 linhas de código
- Build: 0 erros, 0 warnings

---

## 📝 Changelog

### v1.3.0 (2026-02-04)
- ✅ API simplificada: removidos parâmetros `cpf` e `nomePaciente`
- ✅ Extração automática de dados do paciente via IA
- ✅ Tratamento de pacientes não identificados
- ✅ Apenas 7 endpoints essenciais para produção
- ✅ Campo `documentos.paciente_id` nullable

### v1.0.0 (2026-02-03)
- ✅ Lançamento inicial MVP
- ✅ 20 User Stories implementadas
- ✅ Pipeline end-to-end completo
- ✅ API REST pronta para produção

---

## 👤 Autor

**Adjair Farias**
- LinkedIn: [linkedin.com/in/farias-dev](https://linkedin.com/in/farias-dev)
- Email: adjaircfarias@gmail.com
- GitHub: [github.com/adjaircfarias](https://github.com/adjaircfarias)

Desenvolvido com: Clawdex 🔍

---

## 📄 Licença

Este projeto está licenciado sob a Licença MIT.

---

<p align="center">
  <strong>🎊 MVP 100% COMPLETO E FUNCIONAL! 🎊</strong>
</p>

<p align="center">
  Última atualização: Fevereiro 2026 • Versão 1.3.0 • Pronto para Produção
</p>
