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
**Expected:** PostgreSQL with status "Up"

### 2. Ollama Running
```bash
Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get
```
**Expected:** List of models (should include llama3.1:70b)

### 3. API Running
```bash
cd C:\dev\myprojects\ExamAI\src\ExamAI.Api
dotnet run
```
**Expected:** `Now listening on: http://localhost:5076`

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

### Option 3: Via cURL (Windows)

```bash
curl -X POST "http://localhost:5076/api/process-and-save" ^
  -H "accept: application/json" ^
  -F "file=@C:\path\to\exam.pdf"
```

---

## 📊 Real-time Logs

While processing happens, see logs in the terminal where `dotnet run` is running:

```
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'POST /api/process-and-save'
info: Program[0]
      Upload received: exam.pdf (245678 bytes)
info: Program[0]
      Processing completed for document 550e8400-...
```

---

## ❌ Troubleshooting

### "Document already processed" but status "failed"
- **Cause:** Document failed in first processing (e.g., Ollama offline)
- **Duplicate detection:** System detects hash and blocks new upload
- **Solution:** 
  1. Delete failed document: `DELETE /api/exams/{documentId}`
  2. Upload again
- **📖 Complete guide:** [DUPLICATE-FAILED-DOCS.md](DUPLICATE-FAILED-DOCS.md)

### "Failed to fetch"
- **Cause:** CORS not enabled or API not running
- **Solution:** Verify `app.UseCors()` is uncommented in Program.cs

### "File size exceeds 10MB limit"
- **Cause:** File too large
- **Solution:** Reduce size or increase limit in code

### "Invalid file format"
- **Cause:** Unsupported format
- **Solution:** Use only .pdf, .docx or .xlsx

### API crashes with "exit code -1"
- **Cause:** Error in processing (see logs)
- **Solution:** See TROUBLESHOOTING.md

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
5. ✅ Data extracted by Ollama

---

**Last updated:** 2026-02-05 (v1.3.0)

---

<a name="portugues"></a>
## 🇧🇷 Português

## ✅ Pré-requisitos

Antes de testar upload, verifique:

### 1. Docker rodando
```bash
docker-compose ps
```
**Esperado:** PostgreSQL com status "Up"

### 2. Ollama rodando
```bash
Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get
```
**Esperado:** Lista de modelos (deve incluir llama3.1:70b)

### 3. API rodando
```bash
cd C:\dev\myprojects\ExamAI\src\ExamAI.Api
dotnet run
```
**Esperado:** `Now listening on: http://localhost:5076`

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

### Opção 3: Via cURL (Windows)

```bash
curl -X POST "http://localhost:5076/api/process-and-save" ^
  -H "accept: application/json" ^
  -F "file=@C:\caminho\para\exame.pdf"
```

---

## 📊 Logs em Tempo Real

Enquanto o processamento acontece, veja os logs no terminal onde `dotnet run` está rodando:

```
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'POST /api/process-and-save'
info: Program[0]
      Upload received: exame.pdf (245678 bytes)
info: Program[0]
      Processing completed for document 550e8400-...
```

---

## ❌ Troubleshooting

### "Document already processed" mas status "failed"
- **Causa:** Documento falhou no primeiro processamento (ex: Ollama offline)
- **Detecção de duplicata:** Sistema detecta hash e bloqueia novo upload
- **Solução:** 
  1. Deletar documento falhado: `DELETE /api/exams/{documentId}`
  2. Fazer upload novamente
- **📖 Guia completo:** [DUPLICATE-FAILED-DOCS.md](DUPLICATE-FAILED-DOCS.md)

### "Failed to fetch"
- **Causa:** CORS não ativado ou API não rodando
- **Solução:** Verificar se `app.UseCors()` está descomentado em Program.cs

### "File size exceeds 10MB limit"
- **Causa:** Arquivo muito grande
- **Solução:** Reduzir tamanho ou aumentar limite no código

### "Invalid file format"
- **Causa:** Formato não suportado
- **Solução:** Use apenas .pdf, .docx ou .xlsx

### API crashes com "exit code -1"
- **Causa:** Erro no processamento (veja logs)
- **Solução:** Veja TROUBLESHOOTING.md

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
5. ✅ Dados extraídos pelo Ollama

---

**Última atualização:** 05/02/2026 (v1.3.0)
