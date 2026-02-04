# ⚡ ExamAI - Quick Start Guide

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
2. Expandir `POST /api/exams/upload`
3. Click em "Try it out"
4. Upload de um PDF de exame
5. Preencher CPF (opcional)
6. Click "Execute"

**Esperado:** Status 202 Accepted com `documentoId`

---

### Teste 3: Consultar Status

```bash
curl http://localhost:5076/api/exams/status/{documentoId}
```

**Esperado:** Status 200 OK com `"status":"completed"`

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
2. 📖 Ver [PROJECT-COMPLETE.md](docs/PROJECT-COMPLETE.md)
3. 🎯 Testar com exames reais
4. 🔧 Customizar conforme necessário

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
2. Ver [scripts/README.md](scripts/README.md) - Scripts utilitários
3. Ver logs: `docker-compose logs -f`
4. Abrir issue no GitHub

---

**Tempo estimado:** 10-15 minutos (exceto download do modelo Ollama)

**Desenvolvido por:** Adjair Farias + Clawdex 🔍  
**Versão:** 1.2.0
