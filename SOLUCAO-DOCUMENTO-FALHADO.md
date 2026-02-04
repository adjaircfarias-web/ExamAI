# ✅ Solução: Documento com status "failed" bloqueando novos uploads

## 🔍 Seu Problema

Você fez upload do arquivo `Exame_Funcao_Renal_Ricardo_Costa.pdf` e recebeu:

```json
{
  "success": true,
  "duplicate": true,
  "documentoId": "08946c14-2d8e-4e9b-b059-3ad7cc08dccd",
  "status": "failed",
  "message": "Document already processed. Returning cached result.",
  "exames": []
}
```

**Por quê?** O documento foi processado anteriormente mas **falhou** (Ollama com modelo errado: `llama3.1:70b` em vez de `Llama3.1:latest`). O sistema detectou o hash e retornou o resultado "cached" com status "failed".

---

## ✅ Solução Aplicada

### **1. Correção do modelo Ollama** ✅
```json
// src/ExamAI.Api/appsettings.json
"Ollama": {
  "Model": "llama3.1:70b"  → "Llama3.1:latest"
}
```

### **2. Documento falhado deletado** ✅
```bash
DELETE http://localhost:5076/api/exams/08946c14-2d8e-4e9b-b059-3ad7cc08dccd

Resposta:
{
  "success": true,
  "message": "Document deleted successfully"
}
```

### **3. Novos endpoints criados** ✅
- `DELETE /api/exams/{documentoId}` - Deletar documento
- `POST /api/exams/reprocess/{documentoId}` - Reprocessar (limitado)

---

## 🚀 Próximos Passos

### **Agora você pode fazer upload novamente:**

1. **Abrir Swagger:**
   ```
   http://localhost:5076/swagger
   ```

2. **Fazer upload:**
   - Expandir `POST /api/exams/upload`
   - Click "Try it out"
   - Escolher arquivo: `Exame_Funcao_Renal_Ricardo_Costa.pdf`
   - Click "Execute"

3. **Resposta esperada (202 Accepted):**
   ```json
   {
     "success": true,
     "documentoId": "novo-id-aqui",
     "status": "processing"
   }
   ```

4. **Aguardar processamento (10-30 segundos)**

5. **Verificar status:**
   ```bash
   GET /api/exams/status/{documentoId}
   ```

6. **Resultado esperado:**
   ```json
   {
     "status": "completed",
     "examesExtraidos": 3  // Agora deve extrair!
   }
   ```

---

## 📊 Versões Corrigidas

- **v1.2.6** - Correção foreign key (paciente criado antes do documento)
- **v1.2.7** - Correção no /api/process-and-save (mesmo problema)
- **v1.2.8** - Correção modelo Ollama (`llama3.1:70b` → `Llama3.1:latest`)
- **v1.2.9** - Novos endpoints DELETE e reprocess

---

## 🔮 Limitação Conhecida

**Arquivos não são armazenados em disco/blob**, apenas metadados no PostgreSQL.

Por isso:
- ❌ Não é possível reprocessar sem re-upload
- ✅ Solução: DELETE + upload novamente

**Para implementar reprocessamento real seria necessário:**
- Armazenar arquivos em `/uploads/` ou Azure Blob/AWS S3
- Adicionar coluna `FilePath` ou `BlobUrl` na tabela `documentos`
- Modificar endpoint de reprocessamento para ler arquivo armazenado

---

## 📝 Resumo Final

| Item | Status |
|------|--------|
| Modelo Ollama corrigido | ✅ `Llama3.1:latest` |
| Documento falhado deletado | ✅ ID: `08946c14...` |
| Endpoints criados | ✅ DELETE, reprocess |
| Sistema pronto | ✅ Pode fazer upload novamente |

---

**Agora teste novamente!** 🚀

**Guia completo:** [DUPLICATE-FAILED-DOCS.md](DUPLICATE-FAILED-DOCS.md)

**Última atualização:** v1.2.9 - 2026-02-04
