# ♻️ Failed Documents and Duplicate Detection

<p align="center">
  🇺🇸 <a href="#english">English</a> • 🇧🇷 <a href="#portugues">Português</a>
</p>

---

<a name="english"></a>
## 🇺🇸 English

## 🔍 The Problem

When you upload a document and processing **fails** (e.g., Ollama offline, model not found), the document is marked as `"status": "failed"` in the database.

If you try to upload the **same file again**, the system detects a duplicate (by SHA256 hash) and returns:

```json
{
  "success": true,
  "duplicate": true,
  "documentId": "08946c14-...",
  "patientId": null,
  "fileName": "Exam.pdf",
  "message": "Document already processed. Returning cached result.",
  "status": "failed",
  "exams": []
}
```

**Problem:** You cannot reprocess the file because the system thinks it was already processed!

---

## ✅ Solution

### **Step 1: Delete the failed document**

```bash
DELETE http://localhost:5076/api/exams/{documentId}
```

**Example (PowerShell):**
```powershell
Invoke-RestMethod -Uri "http://localhost:5076/api/exams/08946c14-2d8e-4e9b-b059-3ad7cc08dccd" -Method Delete
```

**Response:**
```json
{
  "success": true,
  "message": "Document deleted successfully",
  "documentId": "08946c14-...",
  "fileName": "Exam.pdf"
}
```

---

### **Step 2: Upload again**

Now the hash no longer exists in the database, so you can upload normally:

```bash
POST http://localhost:5076/api/process-and-save
```

**Via Swagger:**
1. `DELETE /api/exams/{documentId}` → Delete failed document
2. `POST /api/process-and-save` → Upload again

---

## 🔧 Via Swagger

### **1. Identify documentId of the failed document**

When you receive the duplicate response:
```json
{
  "duplicate": true,
  "documentId": "08946c14-2d8e-4e9b-b059-3ad7cc08dccd",
  "status": "failed"
}
```

Copy the `documentId`.

---

### **2. Delete document**

1. Open Swagger: `http://localhost:5076/swagger`
2. Expand: `DELETE /api/exams/{documentId}`
3. Click "Try it out"
4. Paste the documentId
5. Click "Execute"

**Expected response (200 OK):**
```json
{
  "success": true,
  "message": "Document deleted successfully"
}
```

---

### **3. Upload again**

1. Expand: `POST /api/process-and-save`
2. Click "Try it out"
3. Choose the file again
4. Click "Execute"

**It should now process successfully!** ✅

---

## ❓ Why isn't there automatic reprocessing?

**Known limitation:** Files are **not stored on disk or blob storage**, only metadata stays in PostgreSQL.

Without the original file, automatic reprocessing is not possible.

**Endpoint created but limited:**
```
POST /api/exams/reprocess/{documentId}
```

**Returns:**
```json
{
  "success": false,
  "error": "Cannot reprocess: original file not stored.",
  "suggestion": "Use DELETE then upload again"
}
```

---

## 🔮 Future Solution

To allow automatic reprocessing, it would be necessary to:

1. **Store files on disk:**
   ```
   /uploads/{documentId}/{filename}
   ```

2. **Or use blob storage (Azure Blob, AWS S3, MinIO):**
   ```json
   {
     "documentId": "...",
     "blobUrl": "https://storage/uploads/file.pdf"
   }
   ```

3. **Reprocessing endpoint would work:**
   ```bash
   POST /api/exams/reprocess/{documentId}
   ```

For now: **DELETE + upload again** is the solution.

---

## 📝 Summary

| Situation | Action |
|-----------|--------|
| Document processed successfully | ✅ Nothing to do |
| Document failed processing | ⚠️ `status: "failed"` |
| Try upload again | ❌ "Document already processed" |
| **Solution** | 1️⃣ `DELETE /api/exams/{id}` + 2️⃣ Upload again |

---

**Last updated:** v1.3.0 - 2026-02-05

---

<a name="portugues"></a>
## 🇧🇷 Português

## 🔍 O Problema

Quando você faz upload de um documento e o processamento **falha** (ex: Ollama offline, modelo não encontrado), o documento fica marcado como `"status": "failed"` no banco.

Se você tentar fazer upload do **mesmo arquivo novamente**, o sistema detecta duplicata (por hash SHA256) e retorna:

```json
{
  "success": true,
  "duplicate": true,
  "documentId": "08946c14-...",
  "patientId": null,
  "fileName": "Exame.pdf",
  "message": "Document already processed. Returning cached result.",
  "status": "failed",
  "exams": []
}
```

**Problema:** Você não consegue reprocessar o arquivo porque o sistema pensa que já foi processado!

---

## ✅ Solução

### **Passo 1: Deletar o documento falhado**

```bash
DELETE http://localhost:5076/api/exams/{documentId}
```

**Exemplo (PowerShell):**
```powershell
Invoke-RestMethod -Uri "http://localhost:5076/api/exams/08946c14-2d8e-4e9b-b059-3ad7cc08dccd" -Method Delete
```

**Resposta:**
```json
{
  "success": true,
  "message": "Document deleted successfully",
  "documentId": "08946c14-...",
  "fileName": "Exame.pdf"
}
```

---

### **Passo 2: Fazer upload novamente**

Agora o hash não existe mais no banco, então você pode fazer upload normalmente:

```bash
POST http://localhost:5076/api/process-and-save
```

**Via Swagger:**
1. `DELETE /api/exams/{documentId}` → Deletar documento falhado
2. `POST /api/process-and-save` → Upload novamente

---

## 🔧 Via Swagger

### **1. Identificar documentId do documento falhado**

Quando você recebe a resposta de duplicata:
```json
{
  "duplicate": true,
  "documentId": "08946c14-2d8e-4e9b-b059-3ad7cc08dccd",
  "status": "failed"
}
```

Copie o `documentId`.

---

### **2. Deletar documento**

1. Abra Swagger: `http://localhost:5076/swagger`
2. Expanda: `DELETE /api/exams/{documentId}`
3. Click "Try it out"
4. Cole o documentId
5. Click "Execute"

**Resposta esperada (200 OK):**
```json
{
  "success": true,
  "message": "Document deleted successfully"
}
```

---

### **3. Upload novamente**

1. Expanda: `POST /api/process-and-save`
2. Click "Try it out"
3. Escolha o arquivo novamente
4. Click "Execute"

**Agora deve processar com sucesso!** ✅

---

## ❓ Por que não tem reprocessamento automático?

**Limitação conhecida:** Os arquivos **não são armazenados em disco ou blob storage**, apenas os metadados ficam no PostgreSQL.

Sem o arquivo original, não é possível reprocessar automaticamente.

**Endpoint criado mas limitado:**
```
POST /api/exams/reprocess/{documentId}
```

**Retorna:**
```json
{
  "success": false,
  "error": "Cannot reprocess: original file not stored.",
  "suggestion": "Use DELETE then upload again"
}
```

---

## 🔮 Solução Futura

Para permitir reprocessamento automático, seria necessário:

1. **Armazenar arquivos em disco:**
   ```
   /uploads/{documentId}/{filename}
   ```

2. **Ou usar blob storage (Azure Blob, AWS S3, MinIO):**
   ```json
   {
     "documentId": "...",
     "blobUrl": "https://storage/uploads/file.pdf"
   }
   ```

3. **Endpoint de reprocessamento funcionaria:**
   ```bash
   POST /api/exams/reprocess/{documentId}
   ```

Por enquanto: **DELETE + upload novamente** é a solução.

---

## 📝 Resumo

| Situação | Ação |
|----------|------|
| Documento processou com sucesso | ✅ Nada a fazer |
| Documento falhou no processamento | ⚠️ `status: "failed"` |
| Tentar upload novamente | ❌ "Document already processed" |
| **Solução** | 1️⃣ `DELETE /api/exams/{id}` + 2️⃣ Upload novamente |

---

**Última atualização:** v1.3.0 - 05/02/2026
