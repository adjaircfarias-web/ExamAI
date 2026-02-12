# 📤 Upload Test Guide

<p align="center">
  🇺🇸 <a href="#english">English</a> • 🇧🇷 <a href="#portugues">Português</a>
</p>

---

<a name="english"></a>
## 🇺🇸 English

## ✅ Prerequisites

Before testing upload, verify:

### 1. Docker Running
```bash
docker-compose ps
```
**Expected:** PostgreSQL and API with status "healthy" or "Up"

### 2. Ollama Running
```bash
curl http://localhost:11434/api/tags
```
**Expected:** List of models (should include phi4:14b or llama3.1:8b)

### 3. API Running
```bash
# API is running via Docker!
curl http://localhost:5076/health
```
**Expected:** `{"status":"healthy"}`

---

## 🧪 Step-by-Step Test

### Option 1: Via Swagger UI (Recommended)

1. **Open Swagger:**
   ```
   http://localhost:5076/swagger
   ```

2. **Expand endpoint:**
   - Look for `POST /api/process-and-save`
   - Click "Try it out"

3. **Fill fields:**
   - `file`: Click "Choose File" and select a PDF

4. **Execute:**
   - Click the blue "Execute" button

5. **Verify response:**
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
         "name": "John Doe",
         "cpf": "12345678900"
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
   - ✅ Status 200 OK = Upload and processing OK
   - ❌ "Failed to fetch" = CORS problem (see troubleshooting)

---

### Option 2: Via PowerShell

```powershell
# Upload
$file = "C:\path\to\your\exam.pdf"
$uri = "http://localhost:5076/api/process-and-save"

$form = @{
    file = Get-Item -Path $file
}

$response = Invoke-RestMethod -Uri $uri -Method Post -Form $form
$documentId = $response.documentId
Write-Host "Upload OK! DocumentId: $documentId"
```

---

### Option 3: Via cURL

```bash
curl -X POST "http://localhost:5076/api/process-and-save" \
  -F "file=@/path/to/exam.pdf"
```

---

## 📊 List All Exams

### Get All Processed Exams
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

## ❌ Troubleshooting

### "Document already processed" but status "failed"
- **Cause:** Document failed in first processing (e.g., Ollama offline)
- **Duplicate detection:** System detects hash and blocks new upload
- **Solution:**
  1. Delete failed document: `DELETE /api/exams/{documentId}`
  2. Upload again

### "Failed to fetch"
- **Cause:** CORS not enabled or API not running
- **Solution:** Verify API is running with `curl http://localhost:5076/health`

### "File size exceeds 10MB limit"
- **Cause:** File too large
- **Solution:** Reduce file size

### "Invalid file format"
- **Cause:** Unsupported format
- **Solution:** Use only .pdf, .docx or .xlsx

### API crashes or returns 500
- **Cause:** Error in processing (check Ollama or database)
- **Solution:**
  ```bash
  # Check API logs
  docker logs examai-api

  # Check Ollama
  curl http://localhost:11434/api/tags

  # Check database
  docker logs examai-postgres
  ```

---

## 🎯 Test Files

Create simple test files:

### Test PDF
Use any PDF with text (not scanned image).

### Test Word (.docx)
```
File: exam-test.docx
Content: "Blood Test: Red Cells 4.5, White Cells 7000"
```

### Test Excel (.xlsx)
```
File: exam-test.xlsx
A1: Exam | B1: Result
A2: Glucose | B2: 95
A3: Cholesterol | C3: 180
```

---

## ✅ Success Checks

1. ✅ Upload returns 200 OK
2. ✅ documentId returned
3. ✅ Processing completes successfully
4. ✅ Data saved in PostgreSQL
5. ✅ Patient data extracted (name and CPF)
6. ✅ Exams and results extracted

---

## 🔗 Related Documentation

- [README.md](README.md) - Complete project documentation
- [QUICK-START.md](QUICK-START.md) - Quick start guide
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Problem solutions

---

**Last updated:** 2026-02-11 (v1.4.0)

---

<a name="portugues"></a>
## 🇧🇷 Português

## ✅ Pré-requisitos

Antes de testar upload, verifique:

### 1. Docker rodando
```bash
docker-compose ps
```
**Esperado:** PostgreSQL e API com status "healthy" ou "Up"

### 2. Ollama rodando
```bash
curl http://localhost:11434/api/tags
```
**Esperado:** Lista de modelos (deve incluir phi4:14b ou llama3.1:8b)

### 3. API rodando
```bash
# A API está rodando via Docker!
curl http://localhost:5076/health
```
**Esperado:** `{"status":"healthy"}`

---

## 🧪 Teste Passo a Passo

### Opção 1: Via Swagger UI (Recomendado)

1. **Abrir Swagger:**
   ```
   http://localhost:5076/swagger
   ```

2. **Expandir endpoint:**
   - Procure `POST /api/process-and-save`
   - Click em "Try it out"

3. **Preencher campos:**
   - `file`: Click "Choose File" e selecione um PDF

4. **Executar:**
   - Click no botão azul "Execute"

5. **Verificar resposta:**
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
         "cpf": "12345678900"
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
   - ✅ Status 200 OK = Upload e processamento OK
   - ❌ "Failed to fetch" = Problema CORS (veja troubleshooting)

---

### Opção 2: Via PowerShell

```powershell
# Fazer upload
$file = "C:\caminho\para\seu\exame.pdf"
$uri = "http://localhost:5076/api/process-and-save"

$form = @{
    file = Get-Item -Path $file
}

$response = Invoke-RestMethod -Uri $uri -Method Post -Form $form
$documentId = $response.documentId
Write-Host "Upload OK! DocumentId: $documentId"
```

---

### Opção 3: Via cURL

```bash
curl -X POST "http://localhost:5076/api/process-and-save" \
  -F "file=@/caminho/para/exame.pdf"
```

---

## 📊 Listar Todos os Exames

### Obter Todos os Exames Processados
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

## ❌ Troubleshooting

### "Document already processed" mas status "failed"
- **Causa:** Documento falhou no primeiro processamento (ex: Ollama offline)
- **Detecção de duplicata:** Sistema detecta hash e bloqueia novo upload
- **Solução:**
  1. Deletar documento falhado: `DELETE /api/exams/{documentId}`
  2. Fazer upload novamente

### "Failed to fetch"
- **Causa:** CORS não ativado ou API não rodando
- **Solução:** Verificar se API está rodando com `curl http://localhost:5076/health`

### "File size exceeds 10MB limit"
- **Causa:** Arquivo muito grande
- **Solução:** Reduzir tamanho do arquivo

### "Invalid file format"
- **Causa:** Formato não suportado
- **Solução:** Use apenas .pdf, .docx ou .xlsx

### API crasha ou retorna 500
- **Causa:** Erro no processamento (verificar Ollama ou banco)
- **Solução:**
  ```bash
  # Verificar logs da API
  docker logs examai-api

  # Verificar Ollama
  curl http://localhost:11434/api/tags

  # Verificar banco
  docker logs examai-postgres
  ```

---

## 🎯 Arquivos de Teste

Crie arquivos de teste simples:

### PDF de teste
Use qualquer PDF com texto (não imagem escaneada).

### Word de teste (.docx)
```
Arquivo: exame-teste.docx
Conteúdo: "Hemograma: Hemácias 4.5, Leucócitos 7000"
```

### Excel de teste (.xlsx)
```
Arquivo: exame-teste.xlsx
A1: Exame | B1: Resultado
A2: Glicemia | B2: 95
A3: Colesterol | C3: 180
```

---

## ✅ Verificações de Sucesso

1. ✅ Upload retorna 200 OK
2. ✅ documentId retornado
3. ✅ Processamento completa com sucesso
4. ✅ Registro salvo no PostgreSQL
5. ✅ Dados do paciente extraídos (nome e CPF)
6. ✅ Exames e resultados extraídos

---

## 🔗 Documentação Relacionada

- [README.md](README.md) - Documentação completa do projeto
- [QUICK-START.md](QUICK-START.md) - Guia de início rápido
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Soluções de problemas

---

**Última atualização:** 11/02/2026 (v1.4.0)
