# 🧪 Guia de Testes - ExamAI

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
- Seções: Exams, Health, Test

---

## 🎯 Teste 3: Upload via Swagger (Recomendado)

### Passo a Passo:

1. **Abrir Swagger:** http://localhost:5076/swagger

2. **Expandir:** `POST /api/exams/upload`

3. **Click:** "Try it out"

4. **Preencher:**
   - `file`: Click "Choose File" → Selecionar PDF/Word/Excel
   - `cpf`: (opcional) Ex: `12345678900`
   - `nomePaciente`: (opcional) Ex: `João Silva`

5. **Click:** "Execute"

6. **Aguardar:** 10-30 segundos (LLM processando)

7. **Ver Response:**

**Sucesso (202 Accepted):**
```json
{
  "success": true,
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "processing",
  "message": "Document accepted for processing",
  "statusUrl": "/api/exams/status/550e8400-..."
}
```

**Copiar o `documentoId` para o próximo teste!**

---

## 🎯 Teste 4: Consultar Status

### Via Swagger:

1. **Expandir:** `GET /api/exams/status/{documentoId}`

2. **Click:** "Try it out"

3. **Colar documentoId** (do teste anterior)

4. **Click:** "Execute"

**Response esperada:**

```json
{
  "success": true,
  "documentoId": "550e8400-...",
  "status": "completed",
  "fileName": "exame.pdf",
  "examesExtraidos": 5
}
```

---

## 🎯 Teste 5: Buscar Exames por CPF

### Via Swagger:

1. **Expandir:** `GET /api/exams/paciente/{cpf}`

2. **Click:** "Try it out"

3. **Preencher cpf:** `12345678900`

4. **Click:** "Execute"

**Response esperada:**

```json
{
  "success": true,
  "paciente": {
    "nome": "João Silva",
    "cpf": "12345678900"
  },
  "exames": [
    {
      "id": "...",
      "tipo": "Colesterol Total",
      "dataColeta": "2026-02-03",
      "resultados": [
        {
          "parametro": "Colesterol Total",
          "valor": 210,
          "unidade": "mg/dL",
          "status": "alto"
        }
      ]
    }
  ]
}
```

---

## 🎯 Teste 6: Verificar Dados no pgAdmin

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
                        ├── pacientes
                        ├── documentos
                        ├── tipos_exame
                        ├── exames
                        └── resultados_exame
```

### Executar Query:

```sql
-- Ver todos os pacientes
SELECT * FROM pacientes;

-- Ver todos os documentos
SELECT * FROM documentos;

-- Ver exames extraídos
SELECT 
    e.id,
    t.nome as tipo_exame,
    e.data_coleta,
    COUNT(r.id) as total_resultados
FROM exames e
LEFT JOIN tipos_exame t ON e.tipo_exame_id = t.id
LEFT JOIN resultados_exame r ON r.exame_id = e.id
GROUP BY e.id, t.nome, e.data_coleta;
```

---

## 🎯 Teste 7: Upload Duplicado (Cache)

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
  "documentoId": "550e8400-...",
  "status": "completed",
  "message": "Document already processed"
}
```

**✅ Hash SHA256 funcionando!**

---

## 🎯 Teste 8: Processar e Salvar (Síncrono)

### Via Swagger:

1. **Expandir:** `POST /api/process-and-save`

2. **Click:** "Try it out"

3. **Upload arquivo:** PDF/Word/Excel

4. **Click:** "Execute"

5. **Aguardar:** 10-30 segundos

**Response esperada (200 OK):**

```json
{
  "success": true,
  "duplicate": false,
  "documentoId": "...",
  "fileName": "exame.pdf",
  "fileHash": "abc123...",
  "data": {
    "paciente": {...},
    "exames": [...]
  },
  "stats": {
    "duration": 12500,
    "documentsProcessed": 1,
    "examsExtracted": 5
  }
}
```

---

## 🎯 Teste 9: Endpoints de Teste

### Via Swagger:

Testar endpoints na seção **Test**:

1. `POST /test/full-pipeline` - Pipeline completo
2. `POST /test/parse-document` - Apenas parsing
3. `POST /test/extract-from-text` - Apenas extração
4. `GET /test/supported-formats` - Formatos suportados

---

## 📊 Checklist Completo de Testes

- [ ] ✅ Health checks (geral, ollama, database)
- [ ] ✅ Swagger UI acessível
- [ ] ✅ Upload de PDF
- [ ] ✅ Upload de Word (.docx)
- [ ] ✅ Upload de Excel (.xlsx)
- [ ] ✅ Consultar status
- [ ] ✅ Buscar por CPF
- [ ] ✅ Upload duplicado (cache)
- [ ] ✅ Verificar dados no pgAdmin
- [ ] ✅ Processar e salvar (síncrono)
- [ ] ✅ Endpoints de teste

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
**Versão:** 1.2.2  
**Data:** 04/02/2026
