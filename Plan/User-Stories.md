# 📝 User Stories - Medical Exam Extractor API

**Projeto:** MedicalExamExtractor API  
**Data:** 29/01/2026  
**Versão:** 1.0 (MVP Simplificado)  
**Autor:** Adjair Farias (com Clawdex 🔍)

---

## 📌 Épicos

1. **Setup de Infraestrutura** - Configuração inicial do projeto
2. **Parsing de Documentos** - Extração de texto de PDF/Word/Excel
3. **Extração com IA** - Usar Ollama para extrair dados estruturados
4. **Persistência de Dados** - Salvar no PostgreSQL
5. **API REST** - Endpoints para upload e consulta

---

## 🎯 Épico 1: Setup de Infraestrutura

### US-001: Criar estrutura do projeto .NET
**Como** desenvolvedor  
**Quero** criar a solution e projetos organizados por camadas  
**Para** ter uma arquitetura limpa e escalável

**Critérios de Aceitação:**
- [ ] Solution criada com 4 projetos (Api, Application, Domain, Infrastructure)
- [ ] Referências entre projetos configuradas
- [ ] .gitignore configurado
- [ ] README.md básico criado

**Tarefas Técnicas:**
```bash
dotnet new sln -n MedicalExamExtractor
dotnet new webapi -n MedicalExamExtractor.Api
dotnet new classlib -n MedicalExamExtractor.Application
dotnet new classlib -n MedicalExamExtractor.Domain
dotnet new classlib -n MedicalExamExtractor.Infrastructure
dotnet sln add **/*.csproj
```

---

### US-002: Configurar banco PostgreSQL
**Como** desenvolvedor  
**Quero** ter um banco PostgreSQL rodando e configurado  
**Para** persistir os dados dos exames

**Critérios de Aceitação:**
- [ ] PostgreSQL rodando (Docker ou local)
- [ ] Database `medicalexams` criada
- [ ] Connection string configurada no appsettings.json
- [ ] EF Core configurado no projeto Infrastructure

**Tarefas Técnicas:**
```bash
# Docker
docker run --name postgres-medical \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=medicalexams \
  -p 5432:5432 \
  -d postgres:16-alpine

# Pacotes NuGet
dotnet add Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Tools
```

---

### US-003: Criar modelo de dados e migrations
**Como** desenvolvedor  
**Quero** ter as tabelas do banco criadas via migrations  
**Para** garantir versionamento do schema

**Critérios de Aceitação:**
- [ ] Entidades criadas (Paciente, Documento, Exame, ResultadoExame, TipoExame)
- [ ] AppDbContext configurado
- [ ] Migration inicial criada e aplicada
- [ ] Seed de tipos_exame inserido

**Tarefas Técnicas:**
```bash
# Criar migration
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Api

# Aplicar migration
dotnet ef database update --project Infrastructure --startup-project Api
```

**Schema:**
- pacientes (id, nome, cpf, data_nascimento)
- documentos (id, paciente_id, nome_arquivo, tipo_arquivo, hash_sha256, status_processamento)
- tipos_exame (id, nome, categoria)
- exames (id, documento_id, tipo_exame_id, data_coleta, medico_solicitante)
- resultados_exame (id, exame_id, parametro, valor_numerico, unidade, referencia_min, referencia_max, status)

---

### US-004: Configurar integração com Ollama
**Como** desenvolvedor  
**Quero** integrar o Ollama no projeto .NET  
**Para** usar o LLM local na extração de dados

**Critérios de Aceitação:**
- [ ] Pacote Microsoft.Extensions.AI.Ollama instalado
- [ ] IChatClient configurado no Program.cs
- [ ] Ollama URL e modelo configurados no appsettings.json
- [ ] Health check endpoint criado (/health/ollama)
- [ ] Teste manual funcionando (curl para health check retorna 200)

**Tarefas Técnicas:**
```bash
dotnet add Api package Microsoft.Extensions.AI --prerelease
dotnet add Api package Microsoft.Extensions.AI.Ollama --prerelease
```

**appsettings.json:**
```json
{
  "Ollama": {
    "Url": "http://localhost:11434",
    "Model": "llama3.1:8b",
    "Temperature": 0.1,
    "MaxTokens": 4096
  }
}
```

---

## 🎯 Épico 2: Parsing de Documentos

### US-005: Implementar parser de PDF
**Como** sistema  
**Quero** extrair texto de arquivos PDF  
**Para** enviar para o LLM processar

**Critérios de Aceitação:**
- [ ] Classe PdfParser implementada
- [ ] Extração de texto funcionando para PDFs simples (não escaneados)
- [ ] Tratamento de erro para PDFs corrompidos
- [ ] Teste manual com 3 PDFs reais

**Tarefas Técnicas:**
```bash
dotnet add Infrastructure package itext7
```

**Interface:**
```csharp
public interface IDocumentParser
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileType);
}
```

---

### US-006: Implementar parser de Word
**Como** sistema  
**Quero** extrair texto de arquivos .docx  
**Para** enviar para o LLM processar

**Critérios de Aceitação:**
- [ ] Classe WordParser implementada
- [ ] Extração de texto funcionando
- [ ] Tratamento de erro para arquivos corrompidos
- [ ] Teste manual com 3 documentos Word reais

**Tarefas Técnicas:**
```bash
dotnet add Infrastructure package DocumentFormat.OpenXml
```

---

### US-007: Implementar parser de Excel
**Como** sistema  
**Quero** extrair texto de arquivos .xlsx  
**Para** enviar para o LLM processar

**Critérios de Aceitação:**
- [ ] Classe ExcelParser implementada
- [ ] Extração de todas as células (formato tabular)
- [ ] Tratamento de erro para arquivos corrompidos
- [ ] Teste manual com 3 planilhas reais

**Tarefas Técnicas:**
```bash
dotnet add Infrastructure package EPPlus
```

---

### US-008: Criar DocumentParserAgent
**Como** sistema  
**Quero** ter um agent que decide qual parser usar  
**Para** abstrair a lógica de parsing

**Critérios de Aceitação:**
- [ ] DocumentParserAgent implementado
- [ ] Detecta tipo de arquivo pela extensão
- [ ] Chama o parser correto (PDF/Word/Excel)
- [ ] Retorna texto bruto extraído
- [ ] Lança exceção para formatos não suportados

**Fluxo:**
```
Stream + extensão → DocumentParserAgent → Parser específico → Texto
```

---

## 🎯 Épico 3: Extração com IA

### US-009: Implementar ExtractionAgent com Ollama
**Como** sistema  
**Quero** enviar texto para o Ollama e receber JSON estruturado  
**Para** extrair dados dos exames

**Critérios de Aceitação:**
- [ ] ExtractionAgent implementado
- [ ] System prompt otimizado para exames médicos
- [ ] User prompt com texto do documento
- [ ] Parsing de resposta JSON do LLM
- [ ] Tratamento de resposta malformada (retry 1x)
- [ ] Teste com 10 documentos reais, medir precisão

**Estrutura JSON esperada:**
```json
{
  "paciente": {
    "nome": "João Silva",
    "data_nascimento": "1980-05-15",
    "data_coleta": "2026-01-28",
    "medico_solicitante": "Dra. Maria"
  },
  "exames": [
    {
      "tipo": "Colesterol Total",
      "valor": 210,
      "unidade": "mg/dL",
      "referencia_min": 0,
      "referencia_max": 200,
      "status": "alto",
      "observacoes": null
    }
  ]
}
```

**Meta de Precisão:** >85%

---

### US-010: Implementar ValidationAgent
**Como** sistema  
**Quero** validar os dados extraídos pelo LLM  
**Para** garantir consistência antes de salvar

**Critérios de Aceitação:**
- [ ] ValidationAgent implementado
- [ ] Validações básicas:
  - Valor numérico é realmente número
  - Status é um dos permitidos (normal, baixo, alto, crítico)
  - Unidade não está vazia
  - CPF válido (se presente)
- [ ] Retorna lista de warnings (não bloqueia)
- [ ] Logs de validação

---

### US-011: Implementar NormalizationAgent
**Como** sistema  
**Quero** normalizar os dados extraídos  
**Para** padronizar nomenclatura e unidades

**Critérios de Aceitação:**
- [ ] NormalizationAgent implementado
- [ ] Normalização de nomes de exames:
  - "Col. Total" → "Colesterol Total"
  - "Glicemia Jejum" → "Glicemia em Jejum"
  - "TGO" → "TGO (AST)"
- [ ] Mapeamento para tipos_exame (lookup na tabela)
- [ ] Conversão de unidades (opcional MVP)

---

### US-012: Implementar MedicalExamPipeline (Orquestrador)
**Como** sistema  
**Quero** orquestrar todos os agents em sequência  
**Para** processar um documento do início ao fim

**Critérios de Aceitação:**
- [ ] MedicalExamPipeline implementado
- [ ] Fluxo completo funcional:
  1. DocumentParserAgent (texto bruto)
  2. ExtractionAgent (JSON estruturado)
  3. ValidationAgent (verificar dados)
  4. NormalizationAgent (padronizar)
  5. Retornar ExamResult
- [ ] Logs em cada etapa
- [ ] Tratamento de erro (para pipeline em qualquer etapa)

**Interface:**
```csharp
public class MedicalExamPipeline
{
    public async Task<ExamResult> ProcessAsync(
        Stream fileStream, 
        string fileType,
        CancellationToken ct = default);
}
```

---

## 🎯 Épico 4: Persistência de Dados

### US-013: Implementar repositório de dados
**Como** sistema  
**Quero** salvar dados extraídos no PostgreSQL  
**Para** persistir histórico de exames

**Critérios de Aceitação:**
- [ ] ExamRepository implementado
- [ ] Métodos:
  - SaveExamAsync(ExamResult, documentId)
  - GetExamsByPacienteAsync(cpf, filtros)
  - GetExamByIdAsync(exameId)
- [ ] Transações (salvar paciente + documento + exames atomicamente)
- [ ] Tratamento de CPF duplicado (buscar paciente existente)

---

### US-014: Implementar hash de documentos
**Como** sistema  
**Quero** detectar documentos duplicados via SHA256  
**Para** evitar reprocessamento

**Critérios de Aceitação:**
- [ ] Calcular SHA256 do arquivo no upload
- [ ] Verificar se hash já existe na tabela documentos
- [ ] Se existe, retornar resultado anterior (sem reprocessar)
- [ ] Se não existe, prosseguir com pipeline

---

## 🎯 Épico 5: API REST

### US-015: Implementar endpoint de upload
**Como** usuário da API  
**Quero** fazer upload de um documento médico  
**Para** extrair dados automaticamente

**Endpoint:** `POST /api/exams/upload`

**Critérios de Aceitação:**
- [ ] Recebe IFormFile (multipart/form-data)
- [ ] Recebe CPF do paciente (obrigatório)
- [ ] Recebe nome do paciente (opcional)
- [ ] Validações:
  - Arquivo não vazio
  - Tamanho máx 10MB
  - Extensão válida (.pdf, .docx, .xlsx)
  - CPF válido
- [ ] Salva documento na tabela com status "processing"
- [ ] Chama pipeline assíncrono
- [ ] Retorna 202 Accepted com documentoId
- [ ] Em caso de sucesso, atualiza status para "completed"
- [ ] Em caso de erro, atualiza status para "failed" + erro_processamento

**Request:**
```http
POST /api/exams/upload
Content-Type: multipart/form-data

file: [binary]
cpf: 12345678900
nomePaciente: João Silva
```

**Response (202):**
```json
{
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "pacienteId": "660e8400-e29b-41d4-a716-446655440001",
  "status": "processing"
}
```

---

### US-016: Implementar endpoint de status
**Como** usuário da API  
**Quero** consultar o status de processamento de um documento  
**Para** saber se já foi concluído

**Endpoint:** `GET /api/exams/status/{documentoId}`

**Critérios de Aceitação:**
- [ ] Busca documento por ID
- [ ] Retorna status atual (pending, processing, completed, failed)
- [ ] Se completed, retorna quantidade de exames extraídos
- [ ] Se failed, retorna mensagem de erro
- [ ] Retorna 404 se documento não existe

**Response (200):**
```json
{
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "processedAt": "2026-01-29T15:25:00Z",
  "examesExtraidos": 5,
  "erros": []
}
```

---

### US-017: Implementar endpoint de consulta por paciente
**Como** usuário da API  
**Quero** buscar todos os exames de um paciente pelo CPF  
**Para** visualizar histórico médico

**Endpoint:** `GET /api/exams/paciente/{cpf}`

**Critérios de Aceitação:**
- [ ] Busca paciente por CPF
- [ ] Retorna lista de exames com resultados
- [ ] Suporta filtros opcionais:
  - dataInicio (yyyy-MM-dd)
  - dataFim (yyyy-MM-dd)
  - tipoExame (nome)
- [ ] Retorna 404 se paciente não existe
- [ ] Retorna 200 com array vazio se não tem exames

**Response (200):**
```json
{
  "paciente": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "nome": "João Silva",
    "cpf": "12345678900",
    "dataNascimento": "1980-05-15"
  },
  "exames": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "tipo": "Lipidograma",
      "dataColeta": "2026-01-28",
      "resultados": [
        {
          "parametro": "Colesterol Total",
          "valor": 210,
          "unidade": "mg/dL",
          "status": "alto"
        }
      ]
    }
  ],
  "total": 1
}
```

---

### US-018: Implementar endpoint de consulta por exame
**Como** usuário da API  
**Quero** buscar detalhes de um exame específico  
**Para** ver todos os resultados

**Endpoint:** `GET /api/exams/{exameId}`

**Critérios de Aceitação:**
- [ ] Busca exame por ID
- [ ] Retorna dados do paciente
- [ ] Retorna todos os resultados do exame
- [ ] Retorna 404 se exame não existe

**Response (200):**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440002",
  "tipo": "Lipidograma",
  "dataColeta": "2026-01-28",
  "medicoSolicitante": "Dra. Maria Santos",
  "paciente": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "nome": "João Silva",
    "cpf": "12345678900"
  },
  "resultados": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440003",
      "parametro": "Colesterol Total",
      "valorNumerico": 210,
      "unidade": "mg/dL",
      "referenciaMin": 0,
      "referenciaMax": 200,
      "status": "alto"
    }
  ]
}
```

---

### US-019: Implementar health checks
**Como** operador  
**Quero** verificar a saúde da API  
**Para** monitorar disponibilidade

**Endpoints:**
- `GET /health` - Health geral
- `GET /health/ollama` - Health do Ollama
- `GET /health/database` - Health do PostgreSQL

**Critérios de Aceitação:**
- [ ] /health retorna 200 se tudo OK
- [ ] /health/ollama testa conexão com Ollama (ping)
- [ ] /health/database testa conexão com PostgreSQL
- [ ] Retorna 503 Service Unavailable se algum serviço está down

**Response (200):**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-29T15:30:00Z",
  "services": {
    "database": "healthy",
    "ollama": "healthy"
  }
}
```

---

### US-020: Configurar Swagger/OpenAPI
**Como** desenvolvedor  
**Quero** ter documentação automática da API  
**Para** facilitar uso e testes

**Critérios de Aceitação:**
- [ ] Swagger configurado no Program.cs
- [ ] Disponível em /swagger
- [ ] Todos os endpoints documentados
- [ ] Exemplos de request/response
- [ ] Descrições dos parâmetros

---

## 🎯 Épico 6: Deploy e Documentação

### US-021: Criar Dockerfile
**Como** DevOps  
**Quero** containerizar a aplicação  
**Para** facilitar deploy

**Critérios de Aceitação:**
- [ ] Dockerfile criado (multi-stage build)
- [ ] Imagem build com sucesso
- [ ] Container roda com sucesso
- [ ] Variáveis de ambiente configuráveis

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedicalExamExtractor.Api.dll"]
```

---

### US-022: Criar docker-compose
**Como** DevOps  
**Quero** subir API + PostgreSQL com um comando  
**Para** simplificar ambiente de desenvolvimento

**Critérios de Aceitação:**
- [ ] docker-compose.yml criado
- [ ] Serviços: api + postgres
- [ ] API conecta no Ollama local (host.docker.internal)
- [ ] API conecta no PostgreSQL do container
- [ ] Volumes persistentes para PostgreSQL
- [ ] `docker-compose up -d` funciona

---

### US-023: Criar documentação do projeto
**Como** desenvolvedor  
**Quero** ter documentação clara do projeto  
**Para** onboarding e manutenção

**Critérios de Aceitação:**
- [ ] README.md completo:
  - Descrição do projeto
  - Pré-requisitos (Ollama, PostgreSQL)
  - Como rodar localmente
  - Como rodar com Docker
  - Exemplos de uso (curl)
  - Estrutura do projeto
- [ ] SETUP.md com instruções detalhadas
- [ ] Comentários no código (classes principais)

---

## 📊 Resumo de Prioridades

### **Sprint 1 (1 semana) - Fundação**
- US-001: Criar estrutura do projeto
- US-002: Configurar PostgreSQL
- US-003: Criar modelo de dados
- US-004: Configurar Ollama

### **Sprint 2 (1 semana) - Parsing**
- US-005: Parser PDF
- US-006: Parser Word
- US-007: Parser Excel
- US-008: DocumentParserAgent

### **Sprint 3 (1,5 semanas) - IA**
- US-009: ExtractionAgent
- US-010: ValidationAgent
- US-011: NormalizationAgent
- US-012: MedicalExamPipeline

### **Sprint 4 (1 semana) - Persistência**
- US-013: Repositório
- US-014: Hash de documentos

### **Sprint 5 (1 semana) - API**
- US-015: Endpoint upload
- US-016: Endpoint status
- US-017: Endpoint consulta por paciente
- US-018: Endpoint consulta por exame
- US-019: Health checks
- US-020: Swagger

### **Sprint 6 (3 dias) - Deploy**
- US-021: Dockerfile
- US-022: Docker Compose
- US-023: Documentação

---

## 📋 Backlog (Fora do MVP)

### **Funcionalidades Futuras**
- Autenticação JWT
- Processamento assíncrono (background jobs)
- Cache de respostas
- Análise de tendências temporais
- Dashboard web
- Exportação de relatórios PDF
- OCR para documentos escaneados
- Fine-tuning do modelo local
- Integração HL7/FHIR

---

## ✅ Definition of Done (DoD)

Uma User Story está **DONE** quando:

1. ✅ Código implementado e funcionando
2. ✅ Testado manualmente (smoke test)
3. ✅ Code review feito (se trabalho em equipe)
4. ✅ Merged na branch main/develop
5. ✅ Documentação atualizada (se aplicável)
6. ✅ Funcionando no ambiente local

**Não incluído no MVP:**
- ❌ Testes unitários
- ❌ Testes de integração
- ❌ Testes de carga
- ❌ Deploy em produção

---

**Última atualização:** 29/01/2026  
**Versão:** 1.0 (MVP Simplificado)  
**Total de User Stories:** 23
