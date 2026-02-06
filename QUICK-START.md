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

### 2️⃣ Start PostgreSQL (Docker)

```bash
# Option A: With script (Windows)
.\scripts\docker-start.ps1

# Option B: Direct command
docker-compose up -d
```

**Wait ~30 seconds** for PostgreSQL to fully initialize.

✅ **Verify:** `docker-compose ps` should show containers "healthy"

---

### 3️⃣ Download Ollama Model

```bash
ollama pull llama3.1:70b
```

⚠️ **Warning:** ~40GB download. May take time!

✅ **Verify:** `ollama list` should list the model

---

### 4️⃣ Apply Migrations

```bash
cd src\ExamAI.Api
dotnet ef database update
```

✅ **Verify:** Should display "Done" at the end

---

### 5️⃣ Run API

```bash
dotnet run
```

✅ **Verify:** 
- Console should show: "Now listening on: http://localhost:5076"
- Swagger: http://localhost:5076/swagger

---

## 🎉 Ready! Now Test

### Test 1: Health Check

```bash
curl http://localhost:5076/health
```

**Expected:** `{"status":"healthy"}`

---

### Test 2: Upload Exam (Swagger UI)

1. Open http://localhost:5076/swagger
2. Expand `POST /api/process-and-save`
3. Click "Try it out"
4. Upload a medical exam PDF
5. Click "Execute"

**Expected:** Status 200 OK with `documentId` and extracted data

---

## 🎯 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| **API** | http://localhost:5076 | - |
| **Swagger** | http://localhost:5076/swagger | - |
| **PostgreSQL** | localhost:5432 | postgres / postgres123 |
| **pgAdmin** | http://localhost:5050 | admin@examai.com / admin123 |

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

### Migrations fail

```bash
# Check if PostgreSQL is accessible
docker exec -it examai-postgres psql -U postgres -d examai

# If OK, try again
dotnet ef database update --force
```

---

## 🛑 Stop Everything

```bash
# Stop API: Ctrl+C in terminal

# Stop Docker
docker-compose down

# Or with script
.\scripts\docker-stop.ps1
```

---

## 📚 Next Steps

After Quick Start is working:

1. 📖 Read complete [README.md](README.md)
2. 🎯 Test with real exams
3. 🔧 Customize as needed

---

## 💡 Tips

- **First execution:** LLM may take 10-30s
- **Development:** Use Swagger UI (easier than curl)
- **View database:** Use pgAdmin (http://localhost:5050)
- **Logs:** `docker-compose logs -f postgres`
- **Reset:** `docker-compose down -v` (deletes data!)

---

## 🆘 Need Help?

1. Check [docker/README.md](docker/README.md) - Docker details
2. Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Problem solutions
3. Check logs: `docker-compose logs -f`
4. Open issue on GitHub

---

**Estimated time:** 10-15 minutes (except Ollama model download)

**Developed by:** Adjair Farias + Clawdex 🔍  
**Version:** 1.3.0

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

### 2️⃣ Iniciar PostgreSQL (Docker)

```bash
# Opção A: Com script (Windows)
.\scripts\docker-start.ps1

# Opção B: Comando direto
docker-compose up -d
```

**Aguarde ~30 segundos** para o PostgreSQL inicializar completamente.

✅ **Verificar:** `docker-compose ps` deve mostrar containers "healthy"

---

### 3️⃣ Baixar Modelo Ollama

```bash
ollama pull llama3.1:70b
```

⚠️ **Atenção:** Download de ~40GB. Pode levar tempo!

✅ **Verificar:** `ollama list` deve listar o modelo

---

### 4️⃣ Aplicar Migrations

```bash
cd src\ExamAI.Api
dotnet ef database update
```

✅ **Verificar:** Deve exibir "Done" ao final

---

### 5️⃣ Rodar a API

```bash
dotnet run
```

✅ **Verificar:** 
- Console deve mostrar: "Now listening on: http://localhost:5076"
- Swagger: http://localhost:5076/swagger

---

## 🎉 Pronto! Agora Teste

### Teste 1: Health Check

```bash
curl http://localhost:5076/health
```

**Esperado:** `{"status":"healthy"}`

---

### Teste 2: Upload de Exame (Swagger UI)

1. Abrir http://localhost:5076/swagger
2. Expandir `POST /api/process-and-save`
3. Click em "Try it out"
4. Fazer upload de um PDF de exame
5. Click "Execute"

**Esperado:** Status 200 OK com `documentId` e dados extraídos

---

## 🎯 Acessos

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **API** | http://localhost:5076 | - |
| **Swagger** | http://localhost:5076/swagger | - |
| **PostgreSQL** | localhost:5432 | postgres / postgres123 |
| **pgAdmin** | http://localhost:5050 | admin@examai.com / admin123 |

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
# Windows: services.msc → PostgreSQL → Stop

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

### Migrations falham

```bash
# Verificar se PostgreSQL está acessível
docker exec -it examai-postgres psql -U postgres -d examai

# Se OK, tentar novamente
dotnet ef database update --force
```

---

## 🛑 Parar Tudo

```bash
# Parar API: Ctrl+C no terminal

# Parar Docker
docker-compose down

# Ou com script
.\scripts\docker-stop.ps1
```

---

## 📚 Próximos Passos

Depois do Quick Start funcionando:

1. 📖 Ler [README.md](README.md) completo
2. 🎯 Testar com exames reais
3. 🔧 Customizar conforme necessário

---

## 💡 Dicas

- **Primeira execução:** LLM pode demorar 10-30s
- **Desenvolvimento:** Use Swagger UI (mais fácil que curl)
- **Ver banco:** Use pgAdmin (http://localhost:5050)
- **Logs:** `docker-compose logs -f postgres`
- **Reset:** `docker-compose down -v` (apaga dados!)

---

## 🆘 Precisa de Ajuda?

1. Ver [docker/README.md](docker/README.md) - Detalhes Docker
2. Ver [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Soluções de problemas
3. Ver logs: `docker-compose logs -f`
4. Abrir issue no GitHub

---

**Tempo estimado:** 10-15 minutos (exceto download do modelo Ollama)

**Desenvolvido por:** Adjair Farias + Clawdex 🔍  
**Versão:** 1.3.0
