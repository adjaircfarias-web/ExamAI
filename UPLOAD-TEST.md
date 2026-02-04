# 📤 Guia de Teste de Upload

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
   - Procure `POST /api/exams/upload`
   - Click em "Try it out"

3. **Preencher campos:**
   - `file`: Click "Choose File" e selecione um PDF
   - `cpf`: (opcional) Ex: "12345678901"
   - `nomePaciente`: (opcional) Ex: "João Silva"

4. **Executar:**
   - Click no botão azul "Execute"

5. **Verificar resposta:**
   ```json
   {
     "success": true,
     "documentoId": "550e8400-e29b-41d4-a716-446655440000",
     "status": "processing",
     "message": "Document accepted for processing",
     "statusUrl": "/api/exams/status/550e8400-..."
   }
   ```
   - ✅ Status 202 Accepted = Upload OK
   - ❌ "Failed to fetch" = CORS problem (veja troubleshooting)

6. **Consultar status do processamento:**
   - Copie o `documentoId` da resposta
   - Expanda `GET /api/exams/status/{documentoId}`
   - Click "Try it out"
   - Cole o documentoId
   - Click "Execute"

7. **Aguardar processamento:**
   - Status muda de `processing` → `completed`
   - Tempo estimado: 10-30 segundos (depende do Ollama)

---

### Opção 2: Via PowerShell

```powershell
# Fazer upload
$file = "C:\caminho\para\seu\exame.pdf"
$uri = "http://localhost:5076/api/exams/upload"

$form = @{
    file = Get-Item -Path $file
    cpf = "12345678901"
    nomePaciente = "João Silva"
}

$response = Invoke-RestMethod -Uri $uri -Method Post -Form $form
$documentoId = $response.documentoId
Write-Host "Upload OK! DocumentoId: $documentoId"

# Consultar status
$statusUri = "http://localhost:5076/api/exams/status/$documentoId"
Invoke-RestMethod -Uri $statusUri -Method Get
```

---

### Opção 3: Via cURL (Windows)

```bash
curl -X POST "http://localhost:5076/api/exams/upload" ^
  -H "accept: application/json" ^
  -F "file=@C:\caminho\para\exame.pdf" ^
  -F "cpf=12345678901" ^
  -F "nomePaciente=João Silva"
```

---

## 📊 Logs em Tempo Real

Enquanto o processamento acontece, veja os logs no terminal onde `dotnet run` está rodando:

```
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'POST /api/exams/upload'
info: Program[0]
      Upload received: exame.pdf (245678 bytes), CPF: 12345678901
info: Program[0]
      Background processing completed for documento 550e8400-...
```

---

## ❌ Troubleshooting

### "Document already processed" mas status "failed"
- **Causa:** Documento falhou no primeiro processamento (ex: Ollama offline)
- **Detecção de duplicata:** Sistema detecta hash e bloqueia novo upload
- **Solução:** 
  1. Deletar documento falhado: `DELETE /api/exams/{documentoId}`
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

### Status fica "processing" para sempre
- **Causa:** Ollama não está rodando ou modelo não carregado
- **Solução:** 
  ```bash
  ollama list
  ollama pull llama3.1:70b
  ```

### ExamAI.Api.exe exited with code -1
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

1. ✅ Upload retorna 202 Accepted
2. ✅ documentoId retornado
3. ✅ Status inicial é "processing"
4. ✅ Status muda para "completed" após alguns segundos
5. ✅ Registro salvo no PostgreSQL
6. ✅ Dados extraídos pelo Ollama

---

**Última atualização:** 2026-02-04 (v1.2.5)
