# ♻️ Documentos Falhados e Detecção de Duplicata

## 🔍 O Problema

Quando você faz upload de um documento e o processamento **falha** (ex: Ollama offline, modelo não encontrado), o documento fica marcado como `"status": "failed"` no banco.

Se você tentar fazer upload do **mesmo arquivo novamente**, o sistema detecta duplicata (por hash SHA256) e retorna:

```json
{
  "success": true,
  "duplicate": true,
  "documentoId": "08946c14-...",
  "status": "failed",
  "message": "Document already processed. Returning cached result.",
  "exames": []
}
```

**Problema:** Você não consegue reprocessar o arquivo porque o sistema pensa que já foi processado!

---

## ✅ Solução

### **Passo 1: Deletar o documento falhado**

```bash
DELETE http://localhost:5076/api/exams/{documentoId}
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
  "documentoId": "08946c14-...",
  "fileName": "Exame_Funcao_Renal_Ricardo_Costa.pdf"
}
```

---

### **Passo 2: Fazer upload novamente**

Agora o hash não existe mais no banco, então você pode fazer upload normalmente:

```bash
POST http://localhost:5076/api/exams/upload
```

**Via Swagger:**
1. `DELETE /api/exams/{documentoId}` → Deletar documento falhado
2. `POST /api/exams/upload` → Upload novamente

---

## 🔧 Via Swagger

### **1. Identificar documentoId do documento falhado**

Quando você recebe a resposta de duplicata:
```json
{
  "duplicate": true,
  "documentoId": "08946c14-2d8e-4e9b-b059-3ad7cc08dccd",
  "status": "failed"
}
```

Copie o `documentoId`.

---

### **2. Deletar documento**

1. Abra Swagger: `http://localhost:5076/swagger`
2. Expanda: `DELETE /api/exams/{documentoId}`
3. Click "Try it out"
4. Cole o documentoId
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

1. Expanda: `POST /api/exams/upload`
2. Click "Try it out"
3. Escolha o arquivo novamente
4. Click "Execute"

**Agora deve processar com sucesso!** ✅

---

## 📊 Verificar Status do Processamento

Após fazer novo upload:

```bash
GET http://localhost:5076/api/exams/status/{documentoId}
```

**Status esperado:**
```json
{
  "status": "processing"  // Aguarde...
}
```

**Depois de alguns segundos:**
```json
{
  "status": "completed",
  "examesExtraidos": 3
}
```

---

## ❓ Por que não tem reprocessamento automático?

**Limitação conhecida:** Os arquivos **não são armazenados em disco ou blob storage**, apenas os metadados ficam no PostgreSQL.

Sem o arquivo original, não é possível reprocessar automaticamente.

**Endpoint criado mas limitado:**
```
POST /api/exams/reprocess/{documentoId}
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
   /uploads/{documentoId}/{filename}
   ```

2. **Ou usar blob storage (Azure Blob, AWS S3, MinIO):**
   ```json
   {
     "documentoId": "...",
     "blobUrl": "https://storage/uploads/file.pdf"
   }
   ```

3. **Endpoint de reprocessamento funcionaria:**
   ```bash
   POST /api/exams/reprocess/{documentoId}
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

**Última atualização:** v1.2.9 - 2026-02-04
