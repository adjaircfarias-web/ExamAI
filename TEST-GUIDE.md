# 🧪 Test Guide - ExamAI

<p align="center">
  🇺🇸 <a href="#english">English</a> • 🇧🇷 <a href="#portugues">Português</a>
</p>

---

<a name="english"></a>
## 🇺🇸 English

**How to test the system step by step**

---

## ✅ Prerequisites

Before testing, ensure:

- [x] Docker Desktop running
- [x] PostgreSQL started (`docker-compose up -d`)
- [x] Ollama running with phi4:14b or llama3.1:8b
- [x] API running (`docker-compose up -d` or `dotnet run`)

---

## 🎯 Test 1: Health Checks

### Via Curl

```bash
# 1. General health
curl http://localhost:5076/health
# Expected: {"status":"healthy"}

# 2. Ollama health
curl http://localhost:5076/health/ollama
# Expected: {"status":"healthy","service":"Ollama",...}

# 3. Database health
curl http://localhost:5076/health/database
# Expected: {"status":"healthy","service":"PostgreSQL",...}
```

### Via Browser

```
http://localhost:5076/health
http://localhost:5076/health/ollama
http://localhost:5076/health/database
```

**If all return "healthy" → ✅ System OK!**

---

## 🎯 Test 2: Swagger UI

### Access Swagger

```
http://localhost:5076/swagger
```

**Should show:**
- Swagger UI interface
- List of endpoints
- Sections: Exams, Health

---

## 🎯 Test 3: Upload via Swagger (Recommended)

### Step by Step:

1. **Open Swagger:** http://localhost:5076/swagger

2. **Expand:** `POST /api/process-and-save`

3. **Click:** "Try it out"

4. **Fill:**
   - `file`: Click "Choose File" → Select PDF/Word/Excel

5. **Click:** "Execute"

6. **Wait:** 10-30 seconds (LLM processing)

7. **Verify Response:**

**Success (200 OK):**
```json
{
  "success": true,
  "duplicate": false,
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "patientId": "6a545cd7-...",
  "fileName": "exame.pdf",
  "fileHash": "abc123...",
  "data": {
    "patient": {
      "name": "João Silva",
      "cpf": "12345678900",
      "birthDate": "1985-03-15"
    },
    "exams": [
      {
        "type": "Hemograma Completo",
        "value": 5.2,
        "unit": "milhões/mm³",
        "status": "normal"
      }
    ]
  },
  "validation": {
    "isValid": true,
    "warningCount": 0
  },
  "stats": {
    "duration": 12500,
    "examsExtracted": 5,
    "validationWarnings": 0
  }
}
```

---

## 🎯 Test 4: List All Exams

### Get All Exams
```bash
curl "http://localhost:5076/api/exams?page=1&pageSize=20"
```

### Filter by Patient Name
```bash
curl "http://localhost:5076/api/exams?patientName=Silva"
```

### Search by CPF
```bash
curl "http://localhost:5076/api/exams/patient/12345678900"
```

---

## 🎯 Test 5: Duplicate Upload (Cache)

### Via Swagger:

1. **Upload the SAME file** from Test 3

2. **Observe:**
   - Returns instantly (< 100ms)
   - `duplicate: true`
   - Cached result

**Expected response:**
```json
{
  "success": true,
  "duplicate": true,
  "documentId": "550e8400-...",
  "status": "completed",
  "message": "Document already processed. Returning cached result."
}
```

**✅ SHA256 Hash working!**

---

## 🎯 Test 6: Delete Document

### Via Swagger:

1. **Expand:** `DELETE /api/exams/{documentId}`

2. **Click:** "Try it out"

3. **Paste documentId**

4. **Click:** "Execute"

**Expected response (200 OK):**
```json
{
  "success": true,
  "message": "Document deleted successfully"
}
```

---

## 📊 Complete Test Checklist

- [ ] ✅ Health checks (general, ollama, database)
- [ ] ✅ Swagger UI accessible
- [ ] ✅ PDF upload
- [ ] ✅ Word (.docx) upload
- [ ] ✅ Excel (.xlsx) upload
- [ ] ✅ CPF extraction from document
- [ ] ✅ List all exams
- [ ] ✅ Filter by patient name
- [ ] ✅ Search by CPF
- [ ] ✅ Duplicate upload (cache)
- [ ] ✅ Delete document

---

## 🐛 Common Problems During Tests

### "Failed to fetch" in Swagger

**Cause:** CORS or API not running

**Solution:**
```bash
# Verify API is running
curl http://localhost:5076/health

# Check Docker status
docker-compose ps
```

---

### Upload takes too long (> 1 minute)

**Cause:** phi4:14b or llama3.1:70b model is heavy

**Normal:**
- First inference: 20-30s (loads model)
- Subsequent inferences: 10-20s

**If very slow (> 2 min):**
- Use smaller model: `llama3.1:8b` (change appsettings.json)
- Increase timeout: `"TimeoutSeconds": 300`

---

### Exams not extracted

**Verify:**

1. **Document has data?**
   - PDF with text (not scanned)
   - Word/Excel with structured data

2. **API logs:**
   ```bash
   docker logs examai-api
   ```

3. **Ollama responding:**
   ```bash
   curl http://localhost:11434/api/tags
   ```

---

### CPF not extracted from document

**Cause:** Document doesn't contain CPF or AI didn't extract it

**Solution:**
- Verify document contains CPF (with dashes or without)
- Check API logs for extraction details
- CPF is optional - patient name is always extracted

---

## 🎉 Successful Test!

If all tests passed:

✅ **System 100% functional!**  
✅ **Ready for production use!**  
✅ **Can process real exams with CPF extraction!**

---

## 📚 Related Documentation

- [README.md](README.md) - Complete documentation
- [QUICK-START.md](QUICK-START.md) - Quick start guide
- [UPLOAD-TEST.md](UPLOAD-TEST.md) - Upload testing guide

---

**Developed by:** Adjair Farias  
**Version:** 1.4.0  
**Date:** 2026-02-11

---

<a name="portugues"></a>
## 🇧🇷 Português

**Como testar o sistema passo a passo**

---

## ✅ Pré-requisitos

Antes de testar, certifique-se:

- [x] Docker Desktop rodando
- [x] PostgreSQL iniciado (`docker-compose up -d`)
- [x] Ollama rodando com phi4:14b ou llama3.1:8b
- [x] API rodando (`docker-compose up -d` ou `dotnet run`)

---

## 🎯 Teste 1: Health Checks

### Via Curl

```bash
# 1. Health geral
curl http://localhost:5076/health
# Esperado: {"status":"healthy"}

# 2. Health Ollama
curl http://localhost:5076/health/ollama
# Esperado: {"status":"healthy","service":"Ollama",...}

# 3. Health Database
curl http://localhost:5076/health/database
# Esperado: {"status":"healthy","service":"PostgreSQL",...}
```

### Via Navegador

```
http://localhost:5076/health
http://localhost:5076/health/ollama
http://localhost:5076/health/database
```

**Se todos retornarem "healthy" → ✅ Sistema OK!**

---

## 🎯 Teste 2: Swagger UI

### Acessar Swagger

```
http://localhost:5076/swagger
```

**Deve mostrar:**
- Interface Swagger UI
- Lista de endpoints
- Seções: Exams, Health

---

## 🎯 Teste 3: Upload via Swagger (Recomendado)

### Passo a Passo:

1. **Abrir Swagger:** http://localhost:5076/swagger

2. **Expandir:** `POST /api/process-and-save`

3. **Click:** "Try it out"

4. **Preencher:**
   - `file`: Click "Choose File" → Selecionar PDF/Word/Excel

5. **Click:** "Execute"

6. **Aguardar:** 10-30 segundos (processamento LLM)

7. **Verificar Response:**

**Sucesso (200 OK):**
```json
{
  "success": true,
  "duplicate": false,
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "patientId": "6a545cd7-...",
  "fileName": "exame.pdf",
  "fileHash": "abc123...",
  "data": {
    "patient": {
      "name": "João Silva",
      "cpf": "12345678900",
      "birthDate": "1985-03-15"
    },
    "exams": [
      {
        "type": "Hemograma Completo",
        "value": 5.2,
        "unit": "milhões/mm³",
        "status": "normal"
      }
    ]
  },
  "validation": {
    "isValid": true,
    "warningCount": 0
  },
  "stats": {
    "duration": 12500,
    "examsExtracted": 5,
    "validationWarnings": 0
  }
}
```

---

## 🎯 Teste 4: Listar Todos os Exames

### Obter Todos os Exames
```bash
curl "http://localhost:5076/api/exams?page=1&pageSize=20"
```

### Filtrar por Nome do Paciente
```bash
curl "http://localhost:5076/api/exams?patientName=Silva"
```

### Buscar por CPF
```bash
curl "http://localhost:5076/api/exams/patient/12345678900"
```

---

## 🎯 Teste 5: Upload Duplicado (Cache)

### Via Swagger:

1. **Fazer upload do MESMO arquivo** do Teste 3

2. **Observar:**
   - Retorna instantaneamente (< 100ms)
   - `duplicate: true`
   - Resultado cacheado

**Response esperada:**
```json
{
  "success": true,
  "duplicate": true,
  "documentId": "550e8400-...",
  "status": "completed",
  "message": "Document already processed. Returning cached result."
}
```

**✅ Hash SHA256 funcionando!**

---

## 🎯 Teste 6: Deletar Documento

### Via Swagger:

1. **Expandir:** `DELETE /api/exams/{documentId}`

2. **Click:** "Try it out"

3. **Colar documentId**

4. **Click:** "Execute"

**Response esperada (200 OK):**
```json
{
  "success": true,
  "message": "Document deleted successfully"
}
```

---

## 📊 Checklist Completo de Testes

- [ ] ✅ Health checks (geral, ollama, database)
- [ ] ✅ Swagger UI acessível
- [ ] ✅ Upload de PDF
- [ ] ✅ Upload de Word (.docx)
- [ ] ✅ Upload de Excel (.xlsx)
- [ ] ✅ Extração de CPF do documento
- [ ] ✅ Listar todos os exames
- [ ] ✅ Filtrar por nome do paciente
- [ ] ✅ Buscar por CPF
- [ ] ✅ Upload duplicado (cache)
- [ ] ✅ Deletar documento

---

## 🐛 Problemas Comuns Durante Testes

### "Failed to fetch" no Swagger

**Causa:** CORS ou API não rodando

**Solução:**
```bash
# Verificar se API está rodando
curl http://localhost:5076/health

# Verificar status Docker
docker-compose ps
```

---

### Upload demora muito (> 1 minuto)

**Causa:** Modelo phi4:14b ou llama3.1:70b é pesado

**Normal:**
- Primeira inferência: 20-30s (carrega modelo)
- Inferências seguintes: 10-20s

**Se muito lento (> 2 min):**
- Usar modelo menor: `llama3.1:8b` (alterar appsettings.json)
- Aumentar timeout: `"TimeoutSeconds": 300`

---

### Exames não são extraídos

**Verificar:**

1. **Documento tem dados?**
   - PDF com texto (não escaneado)
   - Word/Excel com dados estruturados

2. **Logs da API:**
   ```bash
   docker logs examai-api
   ```

3. **Ollama respondendo:**
   ```bash
   curl http://localhost:11434/api/tags
   ```

---

### CPF não extraído do documento

**Causa:** Documento não contém CPF ou IA não extraiu

**Solução:**
- Verificar se documento contém CPF (com ou sem traços)
- Verificar logs da API para detalhes da extração
- CPF é opcional - nome do paciente sempre é extraído

---

## 🎉 Teste Bem-Sucedido!

Se todos os testes passaram:

✅ **Sistema 100% funcional!**  
✅ **Pronto para uso em produção!**  
✅ **Pode processar exames reais com extração de CPF!**

---

## 📚 Documentação Relacionada

- [README.md](README.md) - Documentação completa
- [QUICK-START.md](QUICK-START.md) - Guia de início rápido
- [UPLOAD-TEST.md](UPLOAD-TEST.md) - Guia de testes de upload

---

**Desenvolvido por:** Adjair Farias  
**Versão:** 1.4.0  
**Data:** 11/02/2026
