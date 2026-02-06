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

- [x] Docker Desktop running (green icon)
- [x] PostgreSQL started (`docker-compose up -d`)
- [x] Migrations applied (`dotnet ef database update`)
- [x] Ollama running with llama3.1:70b
- [x] API running (`dotnet run`)

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
  "fileName": "exam.pdf",
  "fileHash": "abc123...",
  "data": {
    "patient": {
      "name": "John Doe"
    },
    "exams": [
      {
        "type": "Blood Test",
        "value": 150,
        "unit": "mg/dL",
        "status": "normal"
      }
    ]
  },
  "stats": {
    "duration": 12500,
    "examsExtracted": 5,
    "validationWarnings": 0
  }
}
```

---

## 🎯 Test 4: Search Exams by CPF

### Via Swagger:

1. **Expand:** `GET /api/exams/patient/{cpf}`

2. **Click:** "Try it out"

3. **Fill cpf:** `12345678900`

4. **Click:** "Execute"

**Expected response:**

```json
{
  "success": true,
  "patient": {
    "name": "John Doe",
    "cpf": "12345678900"
  },
  "exams": [
    {
      "id": "...",
      "type": "Total Cholesterol",
      "collectionDate": "2026-02-03",
      "results": [
        {
          "parameter": "Total Cholesterol",
          "value": 210,
          "unit": "mg/dL",
          "status": "high"
        }
      ]
    }
  ]
}
```

---

## 🎯 Test 5: Verify Data in pgAdmin

### Access pgAdmin:

```
http://localhost:5050
```

**Login:**
- Email: `admin@examai.com`
- Password: `admin123`

### Connect to PostgreSQL:

1. **Right click on "Servers"** → "Register" → "Server"

2. **General tab:**
   - Name: `ExamAI`

3. **Connection tab:**
   - Host: `postgres` (inside Docker) or `localhost` (outside)
   - Port: `5432`
   - Database: `examai`
   - Username: `postgres`
   - Password: `postgres123`

4. **Click "Save"**

### View Tables:

```
Servers
└── ExamAI
    └── Databases
        └── examai
            └── Schemas
                └── public
                    └── Tables
                        ├── patients
                        ├── documents
                        ├── exam_types
                        ├── exams
                        └── exam_results
```

### Execute Query:

```sql
-- View all patients
SELECT * FROM patients;

-- View all documents
SELECT * FROM documents;

-- View extracted exams
SELECT 
    e.id,
    t.name as exam_type,
    e.collection_date,
    COUNT(r.id) as total_results
FROM exams e
LEFT JOIN exam_types t ON e.exam_type_id = t.id
LEFT JOIN exam_results r ON r.exam_id = e.id
GROUP BY e.id, t.name, e.collection_date;
```

---

## 🎯 Test 6: Duplicate Upload (Cache)

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
  "message": "Document already processed"
}
```

**✅ SHA256 Hash working!**

---

## 🎯 Test 7: Delete Document

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

## 🎯 Test 8: Reprocess Failed Document

### Via Swagger:

1. **Expand:** `POST /api/exams/reprocess/{documentId}`

2. **Click:** "Try it out"

3. **Paste failed documentId**

**Expected response:**
```json
{
  "success": false,
  "error": "Cannot reprocess: original file not stored.",
  "suggestion": "Use DELETE then upload again"
}
```

**Note:** File storage not implemented. Use DELETE + upload again.

---

## 📊 Complete Test Checklist

- [ ] ✅ Health checks (general, ollama, database)
- [ ] ✅ Swagger UI accessible
- [ ] ✅ PDF upload
- [ ] ✅ Word (.docx) upload
- [ ] ✅ Excel (.xlsx) upload
- [ ] ✅ Search by CPF
- [ ] ✅ Duplicate upload (cache)
- [ ] ✅ Verify data in pgAdmin
- [ ] ✅ Delete document
- [ ] ✅ Reprocess endpoint

---

## 🐛 Common Problems During Tests

### "Failed to fetch" in Swagger

**Cause:** CORS or API not running

**Solution:**
```bash
# Verify API is running
curl http://localhost:5076/health

# If not, run
cd src/ExamAI.Api
dotnet run

# Reload Swagger (Ctrl+F5)
```

---

### Upload takes too long (> 1 minute)

**Cause:** llama3.1:70b model is heavy

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
   - Check output in terminal
   - Look for errors

3. **Ollama responding:**
   ```bash
   curl http://localhost:11434/api/tags
   ```

---

### pgAdmin doesn't connect

**Verify hostname:**

- **Inside Docker:** Use `postgres`
- **Outside Docker:** Use `localhost`

```bash
# Test connectivity
docker exec examai-postgres pg_isready -U postgres
```

---

## 📸 Example Documents to Test

### PDF Exam

You can create a simple PDF with data like:

```
BLOOD TEST

Patient: John Doe
CPF: 123.456.789-00
Birth Date: 05/15/1980
Collection Date: 02/03/2026

Requesting Physician: Dr. Maria Santos
Laboratory: LabMed

RESULTS:

Complete Blood Count
- Hemoglobin: 14.5 g/dL (Reference: 13-17)
- White Cells: 7000 /mm³ (Reference: 4000-10000)

Lipid Panel
- Total Cholesterol: 210 mg/dL (Reference: < 200)
- HDL: 45 mg/dL (Reference: > 40)
- LDL: 130 mg/dL (Reference: < 100)
- Triglycerides: 175 mg/dL (Reference: < 150)

Glucose
- Fasting Glucose: 95 mg/dL (Reference: 70-100)
```

---

## 🎉 Successful Test!

If all tests passed:

✅ **System 100% functional!**
✅ **Ready for production use!**
✅ **Can process real exams!**

---

## 📚 Next Steps

After successful testing:

1. ✅ Process real medical exams
2. ✅ Adjust prompts if needed (ExtractionAgent.cs)
3. ✅ Configure authentication (if production)
4. ✅ Deploy (Docker Compose makes it easy!)

---

## 💡 Testing Tips

1. **Start simple:** Test health checks first
2. **Use Swagger:** It's easier than curl
3. **Watch the logs:** API terminal shows what's happening
4. **Test duplicates:** See cache working
5. **Use pgAdmin:** Visualize saved data

---

**Developed by:** Adjair Farias + Clawdex 🔍  
**Version:** 1.3.0  
**Date:** 05/02/2026

---

<a name="portugues"></a>
## 🇧🇷 Português

**Como testar o sistema passo a passo**

---

## ✅ Pré-requisitos

Antes de testar, certifique-se:

- [x] Docker Desktop rodando (ícone verde)
- [x] PostgreSQL iniciado (`docker-compose up -d`)
- [x] Migrations aplicadas (`dotnet ef database update`)
- [x] Ollama rodando com llama3.1:70b
- [x] API rodando (`dotnet run`)

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

6. **Aguardar:** 10-30 segundos (LLM processando)

7. **Ver Response:**

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
      "name": "João Silva"
    },
    "exams": [
      {
        "type": "Hemograma",
        "value": 150,
        "unit": "mg/dL",
        "status": "normal"
      }
    ]
  },
  "stats": {
    "duration": 12500,
    "examsExtracted": 5,
    "validationWarnings": 0
  }
}
```

---

## 🎯 Teste 4: Buscar Exames por CPF

### Via Swagger:

1. **Expandir:** `GET /api/exams/patient/{cpf}`

2. **Click:** "Try it out"

3. **Preencher cpf:** `12345678900`

4. **Click:** "Execute"

**Response esperada:**

```json
{
  "success": true,
  "patient": {
    "name": "João Silva",
    "cpf": "12345678900"
  },
  "exams": [
    {
      "id": "...",
      "type": "Colesterol Total",
      "collectionDate": "2026-02-03",
      "results": [
        {
          "parameter": "Colesterol Total",
          "value": 210,
          "unit": "mg/dL",
          "status": "alto"
        }
      ]
    }
  ]
}
```

---

## 🎯 Teste 5: Verificar Dados no pgAdmin

### Acessar pgAdmin:

```
http://localhost:5050
```

**Login:**
- Email: `admin@examai.com`
- Senha: `admin123`

### Conectar ao PostgreSQL:

1. **Click direito em "Servers"** → "Register" → "Server"

2. **Aba General:**
   - Name: `ExamAI`

3. **Aba Connection:**
   - Host: `postgres` (dentro do Docker) ou `localhost` (fora)
   - Port: `5432`
   - Database: `examai`
   - Username: `postgres`
   - Password: `postgres123`

4. **Click "Save"**

### Ver Tabelas:

```
Servers
└── ExamAI
    └── Databases
        └── examai
            └── Schemas
                └── public
                    └── Tables
                        ├── patients
                        ├── documents
                        ├── exam_types
                        ├── exams
                        └── exam_results
```

### Executar Query:

```sql
-- Ver todos os pacientes
SELECT * FROM patients;

-- Ver todos os documentos
SELECT * FROM documents;

-- Ver exames extraídos
SELECT 
    e.id,
    t.name as exam_type,
    e.collection_date,
    COUNT(r.id) as total_results
FROM exams e
LEFT JOIN exam_types t ON e.exam_type_id = t.id
LEFT JOIN exam_results r ON r.exam_id = e.id
GROUP BY e.id, t.name, e.collection_date;
```

---

## 🎯 Teste 6: Upload Duplicado (Cache)

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
  "message": "Document already processed"
}
```

**✅ Hash SHA256 funcionando!**

---

## 🎯 Teste 7: Deletar Documento

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

## 🎯 Teste 8: Reprocessar Documento Falho

### Via Swagger:

1. **Expandir:** `POST /api/exams/reprocess/{documentId}`

2. **Click:** "Try it out"

3. **Colar documentId falho**

**Response esperada:**
```json
{
  "success": false,
  "error": "Cannot reprocess: original file not stored.",
  "suggestion": "Use DELETE then upload again"
}
```

**Nota:** Armazenamento de arquivos não implementado. Use DELETE + upload novamente.

---

## 📊 Checklist Completo de Testes

- [ ] ✅ Health checks (geral, ollama, database)
- [ ] ✅ Swagger UI acessível
- [ ] ✅ Upload de PDF
- [ ] ✅ Upload de Word (.docx)
- [ ] ✅ Upload de Excel (.xlsx)
- [ ] ✅ Buscar por CPF
- [ ] ✅ Upload duplicado (cache)
- [ ] ✅ Verificar dados no pgAdmin
- [ ] ✅ Deletar documento
- [ ] ✅ Endpoint de reprocessar

---

## 🐛 Problemas Comuns Durante Testes

### "Failed to fetch" no Swagger

**Causa:** CORS ou API não rodando

**Solução:**
```bash
# Verificar se API está rodando
curl http://localhost:5076/health

# Se não, rodar
cd src/ExamAI.Api
dotnet run

# Recarregar Swagger (Ctrl+F5)
```

---

### Upload demora muito (> 1 minuto)

**Causa:** Modelo llama3.1:70b é pesado

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
   - Ver output no terminal
   - Procurar por erros

3. **Ollama respondendo:**
   ```bash
   curl http://localhost:11434/api/tags
   ```

---

### pgAdmin não conecta

**Verificar hostname:**

- **Dentro do Docker:** Use `postgres`
- **Fora do Docker:** Use `localhost`

```bash
# Testar conectividade
docker exec examai-postgres pg_isready -U postgres
```

---

## 📸 Exemplos de Documentos para Testar

### PDF de Exame

Você pode criar um PDF simples com dados como:

```
EXAME DE SANGUE

Paciente: João Silva
CPF: 123.456.789-00
Data de Nascimento: 15/05/1980
Data da Coleta: 03/02/2026

Médico Solicitante: Dra. Maria Santos
Laboratório: LabMed

RESULTADOS:

Hemograma Completo
- Hemoglobina: 14.5 g/dL (Referência: 13-17)
- Leucócitos: 7000 /mm³ (Referência: 4000-10000)

Lipidograma
- Colesterol Total: 210 mg/dL (Referência: < 200)
- HDL: 45 mg/dL (Referência: > 40)
- LDL: 130 mg/dL (Referência: < 100)
- Triglicerídeos: 175 mg/dL (Referência: < 150)

Glicemia
- Glicemia de Jejum: 95 mg/dL (Referência: 70-100)
```

---

## 🎉 Teste Bem-Sucedido!

Se todos os testes passaram:

✅ **Sistema 100% funcional!**
✅ **Pronto para uso em produção!**
✅ **Pode processar exames reais!**

---

## 📚 Próximos Passos

Após testar com sucesso:

1. ✅ Processar exames médicos reais
2. ✅ Ajustar prompts se necessário (ExtractionAgent.cs)
3. ✅ Configurar autenticação (se produção)
4. ✅ Deploy (Docker Compose facilita!)

---

## 💡 Dicas de Teste

1. **Comece simples:** Teste com health checks primeiro
2. **Use Swagger:** É mais fácil que curl
3. **Veja os logs:** Terminal da API mostra o que está acontecendo
4. **Teste duplicatas:** Veja o cache funcionando
5. **Use pgAdmin:** Visualize os dados salvos

---

**Desenvolvido por:** Adjair Farias + Clawdex 🔍  
**Versão:** 1.3.0  
**Data:** 05/02/2026
