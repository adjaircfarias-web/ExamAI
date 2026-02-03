# 📋 Testes Manuais - US-008: DocumentParserAgent

## ✅ US-008: Criar DocumentParserAgent

### Implementação Concluída

**Classe:** `ExamAI.Application.Agents.DocumentParserAgent`  
**Responsabilidade:** Orquestrador de parsers - detecta tipo de arquivo e chama o parser correto  
**Parsers Suportados:** PdfParser, WordParser, ExcelParser

### Funcionalidades Implementadas

- ✅ Detecta tipo de arquivo pela extensão
- ✅ Escolhe o parser correto automaticamente
- ✅ Chama o parser apropriado (PDF/Word/Excel)
- ✅ Retorna texto bruto extraído
- ✅ Lança `NotSupportedException` para formatos não suportados
- ✅ Logging detalhado de todas as operações
- ✅ Método `GetSupportedFormats()` para listar formatos suportados
- ✅ Método `IsFormatSupported()` para verificar se formato é válido

### Arquitetura do Fluxo

```
Upload de Arquivo (Stream + nome)
        ↓
DocumentParserAgent
        ↓
Detecta extensão (.pdf, .docx, .xlsx)
        ↓
Busca parser que suporta o formato
        ↓
Parser específico (PdfParser, WordParser, ExcelParser)
        ↓
Texto bruto extraído
        ↓
Retorna para o chamador
```

### Como Testar Manualmente

#### 1. Iniciar a API

```bash
cd C:\dev\myprojects\ExamAI\src\ExamAI.Api
dotnet run
```

API rodará em: `http://localhost:5000` (ou `https://localhost:5001`)

---

#### 2. Testar Formatos Suportados

```bash
curl http://localhost:5000/test/supported-formats
```

**Resposta esperada:**
```json
{
  "supportedFormats": [".docx", ".pdf", ".xlsx"],
  "count": 3
}
```

---

#### 3. Testar Upload de PDF

```bash
curl -X POST http://localhost:5000/test/parse-document \
  -F "file=@C:\temp\exame-sangue.pdf" \
  -H "Content-Type: multipart/form-data"
```

**Resposta esperada:**
```json
{
  "success": true,
  "fileName": "exame-sangue.pdf",
  "fileSize": 12345,
  "extractedChars": 850,
  "extractedText": "--- Página 1 ---\nLABORATÓRIO...",
  "supportedFormats": [".docx", ".pdf", ".xlsx"]
}
```

---

#### 4. Testar Upload de Word

```bash
curl -X POST http://localhost:5000/test/parse-document \
  -F "file=@C:\temp\exame-colesterol.docx"
```

**Resposta esperada:**
```json
{
  "success": true,
  "fileName": "exame-colesterol.docx",
  "fileSize": 8765,
  "extractedChars": 620,
  "extractedText": "LABORATÓRIO TESTE...",
  "supportedFormats": [".docx", ".pdf", ".xlsx"]
}
```

---

#### 5. Testar Upload de Excel

```bash
curl -X POST http://localhost:5000/test/parse-document \
  -F "file=@C:\temp\exames-2026.xlsx"
```

**Resposta esperada:**
```json
{
  "success": true,
  "fileName": "exames-2026.xlsx",
  "fileSize": 15432,
  "extractedChars": 1200,
  "extractedText": "=== Planilha: Hemograma ===\nParâmetro | Valor | Unidade...",
  "supportedFormats": [".docx", ".pdf", ".xlsx"]
}
```

---

#### 6. Testar Formato Não Suportado

```bash
# Criar arquivo de teste
echo "teste" > C:\temp\teste.txt

curl -X POST http://localhost:5000/test/parse-document \
  -F "file=@C:\temp\teste.txt"
```

**Resposta esperada (400 Bad Request):**
```json
{
  "success": false,
  "error": "File type '.txt' is not supported. Supported formats: .docx, .pdf, .xlsx",
  "supportedFormats": [".docx", ".pdf", ".xlsx"]
}
```

---

#### 7. Testar Arquivo Sem Extensão

```bash
# Criar arquivo sem extensão
echo "teste" > C:\temp\arquivo_sem_extensao

curl -X POST http://localhost:5000/test/parse-document \
  -F "file=@C:\temp\arquivo_sem_extensao"
```

**Resposta esperada (400 Bad Request):**
```json
{
  "success": false,
  "error": "Cannot determine file type: 'arquivo_sem_extensao' has no extension",
  "supportedFormats": [".docx", ".pdf", ".xlsx"]
}
```

---

### Teste via PowerShell (Windows)

```powershell
# Testar formatos suportados
Invoke-RestMethod -Uri "http://localhost:5000/test/supported-formats" -Method Get

# Testar upload de PDF
$pdfPath = "C:\temp\exame.pdf"
$form = @{
    file = Get-Item -Path $pdfPath
}
Invoke-RestMethod -Uri "http://localhost:5000/test/parse-document" `
    -Method Post `
    -Form $form `
    -ContentType "multipart/form-data"

# Testar formato não suportado
$txtPath = "C:\temp\teste.txt"
$form = @{
    file = Get-Item -Path $txtPath
}
Invoke-RestMethod -Uri "http://localhost:5000/test/parse-document" `
    -Method Post `
    -Form $form `
    -ContentType "multipart/form-data"
```

---

### Código de Exemplo (C#)

```csharp
using ExamAI.Application.Agents;
using Microsoft.Extensions.Logging;

public class ExampleService
{
    private readonly DocumentParserAgent _parserAgent;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(
        DocumentParserAgent parserAgent,
        ILogger<ExampleService> logger)
    {
        _parserAgent = parserAgent;
        _logger = logger;
    }

    public async Task<string> ProcessUploadedFileAsync(IFormFile file)
    {
        // Verificar se formato é suportado
        if (!_parserAgent.IsFormatSupported(file.FileName))
        {
            var supported = string.Join(", ", _parserAgent.GetSupportedFormats());
            throw new NotSupportedException(
                $"File format not supported. Supported: {supported}");
        }

        // Extrair texto
        using var stream = file.OpenReadStream();
        var extractedText = await _parserAgent.ExtractTextAsync(
            stream, 
            file.FileName);

        _logger.LogInformation(
            "Extracted {CharCount} chars from {FileName}",
            extractedText.Length,
            file.FileName);

        return extractedText;
    }

    public IEnumerable<string> GetSupportedFormats()
    {
        return _parserAgent.GetSupportedFormats();
    }
}
```

---

### Critérios de Aceitação ✅

- [x] DocumentParserAgent implementado
- [x] Detecta tipo de arquivo pela extensão
- [x] Chama o parser correto (PDF/Word/Excel)
- [x] Retorna texto bruto extraído
- [x] Lança exceção para formatos não suportados
- [x] Logging detalhado
- [x] Método auxiliar `GetSupportedFormats()`
- [x] Método auxiliar `IsFormatSupported()`
- [x] Registrado no DI container
- [x] Endpoints de teste criados
- [x] Build sem erros ou warnings

---

### Logs Esperados

Ao processar um documento, você verá logs como:

```
info: ExamAI.Application.Agents.DocumentParserAgent[0]
      Processing document 'exame-sangue.pdf' with extension '.pdf'
      
info: ExamAI.Application.Agents.DocumentParserAgent[0]
      Using parser 'PdfParser' for file 'exame-sangue.pdf'
      
info: ExamAI.Infrastructure.Parsers.PdfParser[0]
      Starting PDF text extraction, size: 12345 bytes
      
info: ExamAI.Infrastructure.Parsers.PdfParser[0]
      PDF has 2 pages
      
info: ExamAI.Infrastructure.Parsers.PdfParser[0]
      PDF text extraction completed, total chars: 850
      
info: ExamAI.Application.Agents.DocumentParserAgent[0]
      Successfully extracted 850 characters from 'exame-sangue.pdf'
```

---

### Tratamento de Erros

#### Formato não suportado
```json
{
  "success": false,
  "error": "File type '.txt' is not supported. Supported formats: .docx, .pdf, .xlsx"
}
```

#### Arquivo corrompido
```json
{
  "success": false,
  "error": "The PDF file is corrupted or invalid"
}
```

#### Arquivo sem extensão
```json
{
  "success": false,
  "error": "Cannot determine file type: 'filename' has no extension"
}
```

---

### Próximos Passos (US-009)

Após validar o DocumentParserAgent, seguir para a **US-009: Implementar ExtractionAgent** que irá:
- Receber o texto extraído
- Enviar para o Ollama (LLM local)
- Processar resposta JSON estruturada
- Extrair dados médicos (paciente, exames, resultados)

---

### Melhorias Futuras (Backlog)

- Suporte para detecção de MIME type (além de extensão)
- Cache de parsers por tipo
- Validação de tamanho máximo de arquivo
- Processamento assíncrono em background
- Suporte para múltiplos arquivos em batch

---

**Data de Implementação:** 03/02/2026 - 23:30  
**Implementado por:** Clawdex 🔍 + Farias  
**Status:** ✅ COMPLETO - Sprint 2 de Parsing Finalizada! 🎉
