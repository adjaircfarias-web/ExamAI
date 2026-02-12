# ⚡ ExamAI - Quick Start Guide

<p align="center">
  🇺🇸 <a href="#english">English</a> • 🇧🇷 <a href="#portugues">Português</a>
</p>

---

<a name="english"></a>
## 🇺🇸 English

**Quick guide to start the project in 5 minutes!**

---

## 🎯 Objective

Start ExamAI locally to test medical exam data extraction with AI.

---

## 📋 Prerequisites Checklist

Before starting, ensure you have installed:

- [ ] **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop/)
- [ ] **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- [ ] **Ollama** - [Download](https://ollama.com)

---

## 🚀 Setup in 5 Steps

### 1️⃣ Clone Repository

```bash
git clone <repo-url>
cd ExamAI
```

---

### 2️⃣ Start with Docker Compose

```bash
# Start everything (PostgreSQL + API)
docker-compose up -d
```

**Wait ~30 seconds** for PostgreSQL to fully initialize.

✅ **Verify:** `docker-compose ps` should show containers "healthy"

---

### 3️⃣ Download Ollama Model

```bash
# Recommended: phi4:14b (~9GB)
ollama pull phi4:14b

# Or smaller model: llama3.1:8b (~5GB)
ollama pull llama3.1:8b
```

✅ **Verify:** `ollama list` should list the model

---

### 4️⃣ Access API

```bash
# API is already running via Docker!
# Just access:
open http://localhost:5076/swagger
```

✅ **Verify:** Swagger UI should load

---

### 5️⃣ Test Health Endpoints

```bash
# API Health
curl http://localhost:5076/health
# Expected: {"status":"healthy"}

# Ollama Health
curl http://localhost:5076/health/ollama
# Expected: {"status":"healthy"}

# Database Health
curl http://localhost:5076/health/database
# Expected: {"status":"healthy"}
```

---

## 🎉 Ready! Now Test

### Test 1: Upload Exam (Swagger UI)

1. Open http://localhost:5076/swagger
2. Expand `POST /api/process-and-save`
3. Click "Try it out"
4. Upload a medical exam PDF
5. Click "Execute"

**Expected:** Status 200 OK with `documentId` and extracted data

---

### Test 2: List All Exams

```bash
curl "http://localhost:5076/api/exams?page=1&pageSize=20"
```

---

### Test 3: Search by CPF

```bash
curl "http://localhost:5076/api/exams/patient/12345678900"
```

---

## 🎯 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| **API** | http://localhost:5076 | - |
| **Swagger** | http://localhost:5076/swagger | - |
| **PostgreSQL** | localhost:5432 | postgres / postgres123 |

---

## 🐛 Common Problems

### Docker doesn't start

```bash
# Check if Docker is running
docker version

# Check logs
docker-compose logs postgres
```

### Port 5432 occupied

```bash
# Stop local PostgreSQL
# Windows: services.msc → PostgreSQL → Stop

# Or change port in docker-compose.yml
ports:
  - "15432:5432"
```

### Ollama not responding

```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# If not, start:
# Windows: Ollama should start automatically
# Linux/Mac: ollama serve
```

### Container fails to start

```bash
# Check API logs
docker logs examai-api

# Check PostgreSQL logs
docker logs examai-postgres
```

---

## 🛑 Stop Everything

```bash
# Stop Docker (including API)
docker-compose down
```

---

## 📚 Next Steps

After Quick Start is working:

1. 📖 Read complete [README.md](README.md)
2. 🎯 Test with real exams
3. 🔧 Customize as needed

---

## 💡 Tips

- **First execution:** LLM may take 10-30s for first request
- **Development:** Use Swagger UI (easier than curl)
- **Logs:** `docker-compose logs -f api`
- **Reset:** `docker-compose down -v` (deletes all data!)

---

## 🆘 Need Help?

1. Check [docker/README.md](docker/README.md) - Docker details
2. Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Problem solutions
3. Check logs: `docker-compose logs -f`
4. Open issue on GitHub

---

**Estimated time:** 5-10 minutes (plus Ollama model download)

**Developed by:** Adjair Farias  
**Version:** 1.4.0

---

<a name="portugues"></a>
## 🇧🇷 Português

**Guia rápido para iniciar o projeto em 5 minutos!**

---

## 🎯 Objetivo

Iniciar o ExamAI localmente para testar extração de dados de exames médicos com IA.

---

## 📋 Checklist de Pré-requisitos

Antes de começar, certifique-se de ter instalado:

- [ ] **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop/)
- [ ] **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- [ ] **Ollama** - [Download](https://ollama.com)

---

## 🚀 Setup em 5 Passos

### 1️⃣ Clonar o Repositório

```bash
git clone <repo-url>
cd ExamAI
```

---

### 2️⃣ Iniciar com Docker Compose

```bash
# Iniciar tudo (PostgreSQL + API)
docker-compose up -d
```

**Aguarde ~30 segundos** para o PostgreSQL inicializar completamente.

✅ **Verificar:** `docker-compose ps` deve mostrar containers "healthy"

---

### 3️⃣ Baixar Modelo Ollama

```bash
# Recomendado: phi4:14b (~9GB)
ollama pull phi4:14b

# Ou modelo menor: llama3.1:8b (~5GB)
ollama pull llama3.1:8b
```

✅ **Verificar:** `ollama list` deve listar o modelo

---

### 4️⃣ Acessar API

```bash
# A API já está rodando via Docker!
# Basta acessar:
open http://localhost:5076/swagger
```

✅ **Verificar:** Swagger UI deve carregar

---

### 5️⃣ Testar Health Endpoints

```bash
# Health da API
curl http://localhost:5076/health
# Esperado: {"status":"healthy"}

# Health do Ollama
curl http://localhost:5076/health/ollama
# Esperado: {"status":"healthy"}

# Health do Banco
curl http://localhost:5076/health/database
# Esperado: {"status":"healthy"}
```

---

## 🎉 Pronto! Agora Teste

### Teste 1: Upload de Exame (Swagger UI)

1. Abrir http://localhost:5076/swagger
2. Expandir `POST /api/process-and-save`
3. Clicar em "Try it out"
4. Fazer upload de um PDF de exame
5. Clicar em "Execute"

**Esperado:** Status 200 OK com `documentId` e dados extraídos

---

### Teste 2: Listar Todos os Exames

```bash
curl "http://localhost:5076/api/exams?page=1&pageSize=20"
```

---

### Teste 3: Buscar por CPF

```bash
curl "http://localhost:5076/api/exams/patient/12345678900"
```

---

## 🎯 Acessos

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **API** | http://localhost:5076 | - |
| **Swagger** | http://localhost:5076/swagger | - |
| **PostgreSQL** | localhost:5432 | postgres / postgres123 |

---

## 🐛 Problemas Comuns

### Docker não inicia

```bash
# Verificar se Docker está rodando
docker version

# Verificar logs
docker-compose logs postgres
```

### Porta 5432 ocupada

```bash
# Parar PostgreSQL local
# Windows: services.msc → PostgreSQL → Parar

# Ou alterar porta no docker-compose.yml
ports:
  - "15432:5432"
```

### Ollama não responde

```bash
# Verificar se Ollama está rodando
curl http://localhost:11434/api/tags

# Se não, iniciar:
# Windows: Ollama deve iniciar automaticamente
# Linux/Mac: ollama serve
```

### Container falha ao iniciar

```bash
# Verificar logs da API
docker logs examai-api

# Verificar logs do PostgreSQL
docker logs examai-postgres
```

---

## 🛑 Parar Tudo

```bash
# Parar Docker (incluindo API)
docker-compose down
```

---

## 📚 Próximos Passos

Depois do Quick Start funcionando:

1. 📖 Ler [README.md](README.md) completo
2. 🎯 Testar com exames reais
3. 🔧 Customizar conforme necessário

---

## 💡 Dicas

- **Primeira execução:** LLM pode demorar 10-30s na primeira requisição
- **Desenvolvimento:** Use Swagger UI (mais fácil que curl)
- **Logs:** `docker-compose logs -f api`
- **Reset:** `docker-compose down -v` (apaga todos os dados!)

---

## 🆘 Precisa de Ajuda?

1. Ver [docker/README.md](docker/README.md) - Detalhes Docker
2. Ver [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Soluções de problemas
3. Ver logs: `docker-compose logs -f`
4. Abrir issue no GitHub

---

**Tempo estimado:** 5-10 minutos (mais download do modelo Ollama)

**Desenvolvido por:** Adjair Farias  
**Versão:** 1.4.0
