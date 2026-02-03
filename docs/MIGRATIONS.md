# 📊 Database Migrations - ExamAI

## Status Atual

✅ **Migration criada:** `InitialCreate`  
⏳ **Migration aplicada:** Não (aguardando PostgreSQL rodar)

---

## 🗄️ Schema Criado

A migration `InitialCreate` cria as seguintes tabelas:

### 1. **pacientes**
- `id` (UUID, PK)
- `nome` (VARCHAR(255), obrigatório)
- `cpf` (VARCHAR(11), único)
- `data_nascimento` (DATE, opcional)
- `created_at` (TIMESTAMP)
- `updated_at` (TIMESTAMP)

**Índices:**
- UNIQUE em `cpf`
- INDEX em `nome`

---

### 2. **documentos**
- `id` (UUID, PK)
- `paciente_id` (UUID, FK → pacientes)
- `nome_arquivo` (VARCHAR(500))
- `tipo_arquivo` (VARCHAR(50))
- `tamanho_bytes` (BIGINT)
- `hash_sha256` (VARCHAR(64))
- `data_upload` (TIMESTAMP)
- `status_processamento` (VARCHAR(50), default: 'pending')
- `erro_processamento` (TEXT, opcional)
- `created_at` (TIMESTAMP)

**Índices:**
- INDEX em `paciente_id`
- INDEX em `hash_sha256`
- INDEX em `status_processamento`

**Delete Cascade:** Ao deletar paciente, deleta todos seus documentos

---

### 3. **tipos_exame**
- `id` (SERIAL, PK)
- `nome` (VARCHAR(255), único)
- `descricao` (TEXT, opcional)
- `categoria` (VARCHAR(100), opcional)
- `created_at` (TIMESTAMP)

**Índices:**
- UNIQUE em `nome`
- INDEX em `categoria`

**Seed Data (10 tipos pré-cadastrados):**
1. Hemograma Completo (Hematologia)
2. Glicemia (Bioquímica)
3. Colesterol Total (Lipidograma)
4. HDL (Lipidograma)
5. LDL (Lipidograma)
6. Triglicerídeos (Lipidograma)
7. Ureia (Função Renal)
8. Creatinina (Função Renal)
9. TGO/AST (Função Hepática)
10. TGP/ALT (Função Hepática)

---

### 4. **exames**
- `id` (UUID, PK)
- `documento_id` (UUID, FK → documentos)
- `tipo_exame_id` (INTEGER, FK → tipos_exame, opcional)
- `data_coleta` (DATE, opcional)
- `medico_solicitante` (VARCHAR(255), opcional)
- `laboratorio` (VARCHAR(255), opcional)
- `created_at` (TIMESTAMP)

**Índices:**
- INDEX em `documento_id`
- INDEX em `tipo_exame_id`
- INDEX em `data_coleta`

**Delete Cascade:** Ao deletar documento, deleta todos seus exames  
**Delete SetNull:** Ao deletar tipo_exame, seta NULL no exame

---

### 5. **resultados_exame**
- `id` (UUID, PK)
- `exame_id` (UUID, FK → exames)
- `parametro` (VARCHAR(255))
- `valor_numerico` (DECIMAL(18,4), opcional)
- `valor_texto` (TEXT, opcional)
- `unidade` (VARCHAR(50), opcional)
- `referencia_min` (DECIMAL(18,4), opcional)
- `referencia_max` (DECIMAL(18,4), opcional)
- `status` (VARCHAR(50), opcional) - valores: 'normal', 'baixo', 'alto', 'crítico'
- `observacoes` (TEXT, opcional)
- `created_at` (TIMESTAMP)

**Índices:**
- INDEX em `exame_id`
- INDEX em `parametro`
- INDEX em `status`

**Delete Cascade:** Ao deletar exame, deleta todos seus resultados

---

## 🚀 Como Aplicar as Migrations

### **1. Certifique-se que o PostgreSQL está rodando**

```bash
# Docker
docker ps | findstr examai-postgres

# Se não estiver rodando:
docker start examai-postgres

# Ou crie um novo:
docker run --name examai-postgres `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres123 `
  -e POSTGRES_DB=examai `
  -p 5432:5432 `
  -d postgres:16-alpine
```

### **2. Aplicar as migrations**

```bash
cd C:\dev\myprojects\ExamAI

dotnet ef database update --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

**Saída esperada:**
```
Build started...
Build succeeded.
Applying migration '20260203012728_InitialCreate'.
Done.
```

### **3. Verificar se as tabelas foram criadas**

```bash
# Conectar ao PostgreSQL
docker exec -it examai-postgres psql -U postgres -d examai

# Listar tabelas
\dt

# Deve mostrar:
# pacientes
# documentos
# tipos_exame
# exames
# resultados_exame
# __EFMigrationsHistory

# Ver estrutura de uma tabela
\d pacientes

# Ver dados seed
SELECT * FROM tipos_exame;

# Sair
\q
```

---

## 🔄 Comandos Úteis de Migration

### **Criar nova migration**
```bash
dotnet ef migrations add NomeDaMigration --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### **Listar migrations**
```bash
dotnet ef migrations list --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### **Remover última migration (se não foi aplicada)**
```bash
dotnet ef migrations remove --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### **Reverter para uma migration específica**
```bash
dotnet ef database update NomeDaMigration --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### **Reverter todas as migrations (DROP DATABASE)**
```bash
dotnet ef database update 0 --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### **Gerar script SQL (sem aplicar)**
```bash
dotnet ef migrations script --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api --output migrations.sql
```

---

## 🐛 Troubleshooting

### Erro: "No project was found"
```bash
# Adicione --project e --startup-project explicitamente
dotnet ef migrations add MigrationName --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### Erro: "Unable to create an object of type 'AppDbContext'"
- Verifique se o `appsettings.json` tem a connection string correta
- Verifique se o PostgreSQL está rodando

### Erro: "password authentication failed"
- Verifique se a senha no `appsettings.json` está correta
- Recrie o container Docker com a senha correta

### Erro: "database does not exist"
```sql
-- Conectar ao PostgreSQL e criar manualmente:
CREATE DATABASE examai;
```

---

## 📝 Próximas Migrations

À medida que o projeto evolui, você pode criar novas migrations para:

- Adicionar novos campos nas tabelas
- Criar novas tabelas
- Modificar índices
- Adicionar constraints
- Popular dados (seed)

**Exemplo:**
```bash
dotnet ef migrations add AddCampoXNaTabelaY --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

---

**Última atualização:** 03/02/2026  
**Migration atual:** InitialCreate (20260203012728)
