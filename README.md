# 🏥 ExamAI - Medical Exam Extractor API

**Status:** ✅ **MVP 100% COMPLETE AND FUNCTIONAL**  
**Version:** 1.3.0  
**Date:** February, 2026

API for automatic and intelligent extraction of medical exam data using local AI (Ollama) + PostgreSQL.

---

## ⚡ Quick Start

> 📖 **First time?** See [QUICK-START.md](QUICK-START.md) - Complete guide in 5 minutes!
> 
> 📤 **Test upload?** See [UPLOAD-TEST.md](UPLOAD-TEST.md) - Step-by-step testing guide!
>
> ♻️ **Document failed?** See [DUPLICATE-FAILED-DOCS.md](DUPLICATE-FAILED-DOCS.md) - How to delete and reprocess

---

### Option 1: Docker Compose (Recommended) 🐳

```bash
# 1. Start PostgreSQL + pgAdmin
docker-compose up -d

# 2. Apply migrations
cd src/ExamAI.Api
dotnet ef database update

# 3. Start Ollama (if already installed)
ollama pull llama3.1:70b

# 4. Run API
dotnet run

# 5. Access Swagger
# http://localhost:5076/swagger
```

### Option 2: Manual Docker

```bash
# 1. Start PostgreSQL
docker run --name examai-postgres \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=examai \
  -p 5432:5432 \
  -v examai_data:/var/lib/postgresql/data \
  -d postgres:16-alpine

# 2-5. Follow the same steps above
```

### Option 3: Using Makefile (Alternative)

```bash
# Complete setup
make setup

# Run API
make run

# See available commands
make help
```

### Option 4: Local PostgreSQL

If you already have PostgreSQL installed locally, just create the database:
```sql
CREATE DATABASE examai;
```

---

## 🚀 Complete Features

### ✅ End-to-End Processing
- **Upload** documents (PDF, Word, Excel)
- **Extraction** automated text extraction (3 specialized parsers)
- **Analysis** with AI (Ollama LLM - llama3.1:70b)
- **Validation** data validation (15+ consistency rules)
- **Normalization** (30+ nomenclature mappings)
- **Persistence** in PostgreSQL with ACID transactions

### ✅ Complete REST API (7 Production Endpoints)

#### Production
- **POST** `/api/process-and-save` - Process and save exam (synchronous)
- **GET** `/api/exams/paciente/{cpf}` - Search exams by CPF (Brazilian ID)
- **POST** `/api/exams/reprocess/{documentoId}` - Reprocess failed document
- **DELETE** `/api/exams/{documentoId}` - Delete document

#### Health & Docs
- **GET** `/health` - General health check
- **GET** `/health/ollama` - Ollama status
- **GET** `/health/database` - PostgreSQL status
- **GET** `/swagger` - Interactive Swagger UI documentation

### ✅ Duplicate Detection
- **SHA256 Hash** for all documents
- **Instant return** for duplicates (< 100ms)
- **Saves** LLM processing and resources

---

## 📊 System Architecture

```
┌─────────────────────────────────────────────────────┐
│                  COMPLETE FLOW                       │
└─────────────────────────────────────────────────────┘

1. Upload → Validations → SHA256
2. Duplicate? → YES: Cache | NO: Continue
3. Parse (PDF/Word/Excel)
4. Extract (Ollama LLM)
5. Validate (15+ rules)
6. Normalize (30+ mappings)
7. Save (PostgreSQL + Transaction)
8. Query (GET endpoints)
```

### Project Structure

```
ExamAI/
├── src/
│   ├── ExamAI.Api/              # REST API + Swagger
│   ├── ExamAI.Application/      # Agents + Pipeline + DTOs
│   ├── ExamAI.Domain/           # Entities + Interfaces
│   └── ExamAI.Infrastructure/   # Parsers + Repository + Services
├── docker/                      # 🐳 Docker configurations
│   ├── postgres/
│   │   ├── Dockerfile           # Custom PostgreSQL image
│   │   └── init/                # Initialization scripts
│   └── README.md                # Docker documentation
├── docs/                        # Complete documentation
│   ├── PROJECT-COMPLETE.md      # 📖 Complete overview
│   ├── PROGRESS.md              # Development history
│   ├── PARSERS.md               # Parser documentation
│   └── SPRINT-*-SUMMARY.md      # Sprint summaries
├── docker-compose.yml           # 🐳 Orchestration (PostgreSQL + pgAdmin)
├── .env.example                 # Environment variables example
├── .dockerignore                # Files ignored in Docker build
└── Plan/                        # Original specification
```

---

## 🔧 Technologies and Libraries

### Backend
- **.NET 10.0** - Main framework
- **C#** - Programming language
- **Entity Framework Core 10** - ORM
- **PostgreSQL 16** - Database
- **Ollama** - Local LLM (llama3.1:70b) 🚀

### Main Libraries
- **iText7** (9.5.0) - PDF parser
- **DocumentFormat.OpenXml** (3.4.1) - Word parser
- **EPPlus** (8.4.1) - Excel parser
- **Microsoft.Extensions.AI** (10.2.0) - LLM client
- **Swashbuckle.AspNetCore** (10.1.1) - Swagger/OpenAPI

### Tools
- **SHA256** - Hash and duplicate detection
- **Transactions** - Data atomicity
- **Dependency Injection** - Inversion of control
- **Structured Logging** - Microsoft.Extensions.Logging

---

## 📖 Complete Documentation

### Setup Guides
1. **[QUICK-START.md](QUICK-START.md)** - ⚡ 5-minute setup
2. **[docker/README.md](docker/README.md)** - 🐳 Complete Docker documentation
3. **[scripts/README.md](scripts/README.md)** - 🛠️ Utility scripts
4. **[TEST-GUIDE.md](TEST-GUIDE.md)** - 🧪 How to test the system
5. **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - 🔧 Solutions for common issues

### Technical Documentation
6. **[PROJECT-COMPLETE.md](docs/PROJECT-COMPLETE.md)** - 📖 Complete MVP overview
7. **[PROGRESS.md](docs/PROGRESS.md)** - History of all 20 User Stories
8. **[PARSERS.md](docs/PARSERS.md)** - Parser documentation
9. **[SPRINT-*-SUMMARY.md](docs/)** - Detailed sprint summaries
10. **[CHANGELOG.md](CHANGELOG.md)** - Version history

### Interactive Documentation
11. **[Swagger UI](http://localhost:5076/swagger)** - API documentation

---

## 🎯 Usage Examples

### 1. Upload and Process Exam

```bash
curl -X POST http://localhost:5076/api/process-and-save \
  -F "file=@blood-test.pdf"
```

**Response (200 OK):**
```json
{
  "success": true,
  "duplicate": false,
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "pacienteId": "660e8400-e29b-41d4-a716-446655440001",
  "fileName": "blood-test.pdf",
  "fileHash": "a3f5b8c9d2e1...",
  "data": {
    "paciente": {
      "nome": "John Doe",
      "dataNascimento": "1985-03-20",
      "dataColeta": "2026-02-03",
      "medicoSolicitante": "Dr. Maria Santos"
    },
    "exames": [
      {
        "tipo": "Total Cholesterol",
        "valor": 210.0,
        "unidade": "mg/dL",
        "referenciaMin": 0.0,
        "referenciaMax": 200.0,
        "status": "high"
      },
      {
        "tipo": "HDL",
        "valor": 45.0,
        "unidade": "mg/dL",
        "referenciaMin": 40.0,
        "referenciaMax": null,
        "status": "normal"
      }
    ]
  },
  "validation": {
    "isValid": true,
    "warningCount": 0,
    "warnings": []
  },
  "stats": {
    "duration": 3521.45,
    "examesExtracted": 5,
    "validationWarnings": 0
  }
}
```

**Note:** If the patient is not identified in the document, the system will create a patient with:
- `nome`: "Paciente não identificado" (Patient not identified)
- `cpf`: null

---

### 2. Search Results by CPF

```bash
curl "http://localhost:5076/api/exams/paciente/12345678900?dataInicio=2026-01-01&dataFim=2026-12-31"
```

**Response (200 OK):**
```json
{
  "success": true,
  "paciente": {
    "id": "660e8400-...",
    "nome": "John Doe",
    "cpf": "12345678900",
    "dataNascimento": "1980-05-15"
  },
  "exames": [
    {
      "id": "770e8400-...",
      "tipo": "Lipid Panel",
      "categoria": "Blood",
      "dataColeta": "2026-02-03",
      "medicoSolicitante": "Dr. Maria Santos",
      "resultados": [
        {
          "parametro": "Total Cholesterol",
          "valor": 210,
          "unidade": "mg/dL",
          "referenciaMin": 0,
          "referenciaMax": 200,
          "status": "high"
        },
        {
          "parametro": "HDL",
          "valor": 45,
          "unidade": "mg/dL",
          "referenciaMin": 40,
          "referenciaMax": null,
          "status": "normal"
        }
      ]
    }
  ],
  "total": 5
}
```

---

### 3. Duplicate Upload (Cache)

```bash
# Upload the same file again
curl -X POST http://localhost:5076/api/process-and-save \
  -F "file=@blood-test.pdf"
```

**Response (200 OK - INSTANT < 100ms):**
```json
{
  "success": true,
  "duplicate": true,
  "documentoId": "550e8400-...",
  "pacienteId": "660e8400-...",
  "fileName": "blood-test.pdf",
  "message": "Document already processed. Returning cached result.",
  "status": "completed",
  "processedAt": "2026-02-04T01:00:00Z",
  "exames": [
    {
      "id": "770e8400-...",
      "tipo": "Lipid Panel",
      "dataColeta": "2026-02-03",
      "resultadosCount": 5
    }
  ]
}
```

---

### 4. Delete Failed Document

```bash
curl -X DELETE http://localhost:5076/api/exams/550e8400-e29b-41d4-a716-446655440000
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Document deleted successfully",
  "documentoId": "550e8400-...",
  "fileName": "blood-test.pdf"
}
```

---

## 🏆 Project Status

| Sprint | Description | Status | USs |
|--------|-------------|--------|-----|
| 1 | **Setup** (PostgreSQL, Ollama, EF Core) | ✅ Complete | 4/4 |
| 2 | **Parsing** (PDF, Word, Excel) | ✅ Complete | 4/4 |
| 3 | **AI Extraction** (LLM + Pipeline) | ✅ Complete | 4/4 |
| 4 | **Persistence** (Database + Hash) | ✅ Complete | 2/2 |
| 5 | **REST API** (Endpoints + Swagger) | ✅ Complete | 6/6 |
| **MVP TOTAL** | | **✅ 100%** | **20/20** |

### 📊 Final Metrics

- **Completed USs:** 20 / 23 (87%)
- **Completed Sprints:** 5 / 5 (MVP 100%)
- **Build Status:** ✅ 0 errors, 0 warnings
- **Production Endpoints:** 7
- **Lines of Code:** ~2500

---

## 💾 Database

### Created Tables

```sql
pacientes           -- Patient data
documentos          -- Uploaded files (with SHA256 hash)
tipos_exame         -- Exam types (seed with 10 types)
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

### Management via pgAdmin 🎯

If you started Docker Compose, you can access pgAdmin:

1. **Access:** http://localhost:5050
2. **Login:**
   - Email: `admin@examai.com`
   - Password: `admin123`
3. **Connect to PostgreSQL:**
   - Host: `postgres` (or `localhost` if external)
   - Port: `5432`
   - Database: `examai`
   - Username: `postgres`
   - Password: `postgres123`

**Visual interface for:**
- ✅ View table structure
- ✅ Execute SQL queries
- ✅ See data in real-time
- ✅ Backup/restore
- ✅ Monitor performance

---

## 🎯 Supported Use Cases

### ✅ Case 1: New Upload
1. User uploads PDF/Word/Excel
2. System validates format and size
3. System calculates SHA256 hash
4. System checks for duplicates
5. System processes with LLM (Ollama)
6. System validates (15+ rules)
7. System normalizes (30+ mappings)
8. System saves to PostgreSQL
9. Returns results immediately

### ✅ Case 2: Duplicate Detected
1. User uploads the same file
2. System calculates hash
3. System detects duplicate
4. **Returns cached result instantly**
5. **Does not reprocess** (saves resources!)

### ✅ Case 3: History Query
1. User provides patient's CPF
2. System searches all exams
3. System returns complete list
4. Supports filters (date, exam type)

### ✅ Case 4: Patient Not Identified
1. User uploads document without patient identification
2. System processes normally
3. System creates patient with:
   - Name: "Paciente não identificado"
   - CPF: null
4. Results are still saved and queryable

---

## 🔒 Security and Validations

- ✅ CPF validation with check digits
- ✅ Maximum size validation (10MB)
- ✅ Allowed extensions validation (.pdf, .docx, .xlsx)
- ✅ SHA256 hash for data integrity
- ✅ Robust error handling
- ✅ ACID transactions in database
- ✅ Complete structured logging
- ✅ Graceful handling of missing patient data

---

## 🐳 Docker Setup

The project includes complete Docker Compose configuration!

### What's included:
- ✅ PostgreSQL 16 Alpine (optimized)
- ✅ pgAdmin 4 (web interface - optional)
- ✅ Persistent volumes
- ✅ Health checks
- ✅ Isolated network

### Main commands:

```bash
# Start everything
docker-compose up -d

# View logs
docker-compose logs -f postgres

# Stop
docker-compose down

# Access pgAdmin
http://localhost:5050
# Email: admin@examai.com
# Password: admin123
```

📖 **Complete documentation:** [docker/README.md](docker/README.md)

---

## 🚀 Installation and Setup

### Prerequisites

- **.NET 10 SDK** - https://dotnet.microsoft.com/download
- **Docker & Docker Compose** - https://www.docker.com/get-started (recommended)
- **PostgreSQL 16+** - https://www.postgresql.org/download/ (or Docker)
- **Ollama** - https://ollama.com

### Step by Step

```bash
# 1. Clone repository
git clone <repo-url>
cd ExamAI

# 2. Start PostgreSQL (Docker)
docker run --name postgres-medical \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=examai \
  -p 5432:5432 -d postgres:16-alpine

# 3. Verify Ollama
ollama list
ollama pull llama3.1:70b

# 4. Apply migrations
cd src/ExamAI.Api
dotnet ef database update

# 5. Run API
dotnet run

# 6. Access
# API: http://localhost:5076
# Swagger: http://localhost:5076/swagger
```

---

## 📚 Additional Resources

### Technical Documentation
- [PROJECT-COMPLETE.md](docs/PROJECT-COMPLETE.md) - Complete overview
- [PROGRESS.md](docs/PROGRESS.md) - All 20 implemented USs
- [PARSERS.md](docs/PARSERS.md) - Parser details

### Sprint Summaries
- [SPRINT-1-SUMMARY.md](docs/SPRINT-1-SUMMARY.md) - Setup
- [SPRINT-2-SUMMARY.md](docs/SPRINT-2-SUMMARY.md) - Parsing
- [SPRINT-3-SUMMARY.md](docs/SPRINT-3-SUMMARY.md) - AI Extraction
- [SPRINT-4-SUMMARY.md](docs/SPRINT-4-SUMMARY.md) - Persistence

### Setup Guides
- [SETUP-POSTGRES.md](docs/SETUP-POSTGRES.md) - Database setup
- [SETUP-OLLAMA.md](docs/SETUP-OLLAMA.md) - Ollama setup
- [TEST-OLLAMA.md](docs/TEST-OLLAMA.md) - Integration tests

---

## 🎉 Project Complete!

### ✅ All MVP Features Implemented
- Upload medical documents
- Automatic data extraction
- Validation and normalization
- Database persistence
- Complete REST API
- Swagger/OpenAPI
- Duplicate detection
- Health checks
- Graceful patient identification handling

### 🏆 Production Ready!

Functional and tested end-to-end system, ready to process real medical exams!

---

## 📝 Changelog

### Version 1.3.0 (2026-02-04)
- ✅ **Simplified API:** Removed `cpf` and `nomePaciente` parameters from `/api/process-and-save`
- ✅ **Auto-extraction:** Patient data is now fully extracted from the document by AI
- ✅ **Graceful defaults:** Unidentified patients get `nome: "Paciente não identificado"` and `cpf: null`
- ✅ **Cleaner codebase:** Removed unused test endpoints and repository methods
- ✅ **Database update:** `documentos.paciente_id` is now nullable
- ✅ **Production focus:** Only 7 essential endpoints for production use

### Version 1.0.0 (2026-02-03)
- ✅ Initial MVP release
- ✅ All 20 User Stories implemented
- ✅ Complete end-to-end processing pipeline
- ✅ Production-ready REST API

---

## 👤 Author

**Adjair Farias**
- LinkedIn: [linkedin.com/in/farias-dev](https://linkedin.com/in/farias-dev)
- Email: adjaircfarias@gmail.com
- GitHub: [github.com/adjaircfarias](https://github.com/adjaircfarias)

**Developed with:** Clawdex 🔍 (Claude Sonnet 4.5 via Clawdbot)

---

## 📄 License

This project is licensed under the MIT License.

---

## 🙏 Acknowledgments

- **Ollama** - Amazing local LLM
- **Meta AI** - Llama 3.1
- **.NET Team** - Excellent framework
- **PostgreSQL** - Reliable database
- **Open Source Community**

---

**🎊 MVP 100% COMPLETE AND FUNCTIONAL! 🎊**

*Last update: February 4th, 2026 - 21:00 (GMT-3)*  
*Version: 1.3.0 - Production Ready*
