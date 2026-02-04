# 🔧 Troubleshooting - ExamAI

Soluções para problemas comuns do projeto.

---

## 🐳 Docker

### ❌ Problema: "unable to get image... cannot find the file specified"

**Erro completo:**
```
unable to get image 'dpage/pgadmin4:latest': error during connect: 
Get "http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/v1.51/images/...": 
open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.
```

**Causa:** Docker Desktop não está rodando

**Solução:**

1. **Abrir Docker Desktop**
   - Procurar "Docker Desktop" no menu Iniciar
   - Aguardar até o ícone ficar verde (canto inferior esquerdo)

2. **Verificar se está rodando:**
   ```bash
   docker info
   ```

3. **Se o Docker não abrir:**
   - Reiniciar o computador
   - Reinstalar Docker Desktop
   - Verificar se WSL2 está instalado (necessário no Windows)

4. **Executar novamente:**
   ```bash
   docker-compose up -d
   ```

---

### ❌ Problema: Porta 5432 já está em uso

**Erro:**
```
Error starting userland proxy: listen tcp 0.0.0.0:5432: 
bind: address already in use
```

**Causa:** PostgreSQL local ou outro serviço usando porta 5432

**Solução A - Parar PostgreSQL local:**

```bash
# Windows (PowerShell admin)
Stop-Service postgresql-x64-*

# Ou via services.msc
# Procurar "PostgreSQL" → Click direito → Stop
```

**Solução B - Usar porta diferente:**

1. Editar `docker-compose.yml`:
   ```yaml
   postgres:
     ports:
       - "15432:5432"  # Usar porta 15432
   ```

2. Editar `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=15432;..."
   }
   ```

---

### ❌ Problema: Containers não iniciam

**Verificar logs:**
```bash
docker-compose logs postgres
docker-compose logs pgadmin
```

**Reset completo:**
```bash
docker-compose down -v
docker-compose up -d
```

---

## 🗄️ Migrations

### ❌ Problema: "The model for context changes each time it is built"

**Erro completo:**
```
System.InvalidOperationException: An error was generated for warning 
'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': 
The model for context 'AppDbContext' changes each time it is built. 
This is usually caused by dynamic values used in a 'HasData' call 
(e.g. `new DateTime()`, `Guid.NewGuid()`).
```

**Causa:** Valores dinâmicos no seed de dados (HasData)

**✅ Solução (JÁ APLICADA):**

O problema já foi corrigido! O `DateTime.UtcNow` foi substituído por data fixa:

```csharp
// ❌ ANTES (errado)
new TipoExame { Id = 1, Nome = "...", CreatedAt = DateTime.UtcNow }

// ✅ DEPOIS (correto)
var seedDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
new TipoExame { Id = 1, Nome = "...", CreatedAt = seedDate }
```

**Se o erro persistir:**

1. Remover migration antiga:
   ```bash
   cd src/ExamAI.Infrastructure
   dotnet ef migrations remove --startup-project ../ExamAI.Api --force
   ```

2. Criar nova migration:
   ```bash
   dotnet ef migrations add InitialCreate --startup-project ../ExamAI.Api
   ```

3. Aplicar:
   ```bash
   cd ../ExamAI.Api
   dotnet ef database update
   ```

---

### ❌ Problema: Migrations não aplicam (banco não conecta)

**Verificar se PostgreSQL está rodando:**
```bash
docker-compose ps
```

**Se container estiver "unhealthy":**
```bash
# Ver logs
docker-compose logs postgres

# Reiniciar
docker-compose restart postgres

# Aguardar 30 segundos
docker-compose ps
```

**Testar conexão manualmente:**
```bash
docker exec -it examai-postgres psql -U postgres -d examai
```

---

### ❌ Problema: "Database examai does not exist"

**Solução:**

O banco é criado automaticamente pelo Docker, mas se não existir:

```bash
# Entrar no container
docker exec -it examai-postgres psql -U postgres

# Criar banco
CREATE DATABASE examai;

# Sair
\q
```

---

## 🧪 Ollama

### ❌ Problema: Ollama não responde

**Verificar se está rodando:**
```bash
curl http://localhost:11434/api/tags
```

**Se não responder:**

**Windows:**
- Ollama deve iniciar automaticamente
- Verificar na bandeja do sistema (ícone de lhama)
- Se não estiver, rodar: `ollama serve`

**Linux/Mac:**
```bash
ollama serve
```

---

### ❌ Problema: Modelo llama3.1:70b não encontrado

**Baixar modelo:**
```bash
ollama pull llama3.1:70b
```

**Verificar modelos instalados:**
```bash
ollama list
```

**Se timeout durante download:**
- O modelo tem ~40GB
- Pode levar horas dependendo da internet
- Use `ollama pull llama3.1:8b` para testar (menor)

---

### ❌ Problema: Inferência muito lenta

**Causa:** Modelo 70b é pesado

**Soluções:**

1. **Usar GPU:**
   - Instalar CUDA/ROCm
   - Ollama detectará automaticamente

2. **Usar modelo menor:**
   ```json
   // appsettings.json
   "Ollama": {
     "Model": "llama3.1:8b"  // Mais rápido
   }
   ```

3. **Aumentar timeout:**
   ```json
   "Ollama": {
     "TimeoutSeconds": 300  // 5 minutos
   }
   ```

---

## 🔨 Build

### ❌ Problema: Warnings de null-safety

**Warnings comuns:**
```
CS8602: Dereference of a possibly null reference
```

**Causa:** C# 10 Nullable Reference Types habilitado

**Solução:** Não afetam funcionalidade, mas podem ser corrigidos:

1. Adicionar null checks:
   ```csharp
   if (resultado?.Paciente != null)
   {
       // usar resultado.Paciente
   }
   ```

2. Ou suprimir (não recomendado):
   ```csharp
   resultado!.Paciente  // forçar non-null
   ```

---

## 🌐 API

### ❌ Problema: PostgresException 23503 - Foreign Key Violation

**Erro:**
```
PostgresException: 23503
insert or update on table "documentos" violates foreign key constraint 
"FK_documentos_pacientes_paciente_id"
```

**Causa:** Tentativa de criar Documento sem Paciente válido

**✅ Solução (JÁ APLICADA - v1.2.6):**

O endpoint de upload agora cria/busca paciente ANTES de criar o documento.

**Comportamento correto:**
1. Se CPF fornecido → busca paciente existente ou cria novo
2. Se CPF não fornecido → cria paciente anônimo
3. Documento sempre criado com `PacienteId` válido

---

### ❌ Problema: API crasheando ao fazer upload de arquivo (exit code -1)

**Erro:**
```
ExamAI.Api.exe exited with code -1 (0xffffffff)
Acontece ao fazer upload de PDF/Word/Excel
```

**Causa:** IFormFile sendo acessado em Task.Run após request HTTP ter terminado

**✅ Solução (JÁ APLICADA):**

O problema foi corrigido! Agora o arquivo é copiado para memória ANTES de retornar 202 Accepted.

**O que mudou:**
1. Arquivo copiado para `byte[]` antes de iniciar Task.Run
2. Novo scope criado dentro do Task.Run para dependências
3. Status do documento atualizado corretamente

**Se ainda ocorrer:**

1. **Verificar se Ollama está rodando:**
   ```bash
   Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get
   ```

2. **Verificar logs da API** (terminal onde está rodando `dotnet run`)
   - Procure por "Background processing failed"
   - Veja a exceção específica

3. **Verificar status do documento:**
   ```bash
   Invoke-RestMethod -Uri "http://localhost:5076/api/exams/status/{documentoId}" -Method Get
   ```

4. **Testar processamento síncrono** (endpoint alternativo):
   ```bash
   # Use /api/process-and-save em vez de /api/exams/upload
   ```

---

### ❌ Problema: API crasheando ao iniciar (exit code -1)

**Erro:**
```
C:\...\ExamAI.Api.exe exited with code -1 (0xffffffff)
```

**Causa:** Problema na inicialização do Ollama Client com try-catch

**✅ Solução (JÁ APLICADA):**

O problema foi corrigido removendo try-catch desnecessário na configuração do Ollama Client. O cliente não valida conexão no construtor.

**Se ainda ocorrer:**

1. **Verificar se Docker está rodando** (PostgreSQL):
   ```bash
   docker-compose ps
   ```

2. **Verificar logs detalhados:**
   ```bash
   cd src/ExamAI.Api
   dotnet run --verbosity detailed
   ```

3. **Verificar se Ollama está rodando:**
   ```bash
   Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get
   ```

4. **Verificar connection string do PostgreSQL:**
   - appsettings.json deve ter configuração correta
   - PostgreSQL deve estar acessível na porta 5432

---

### ❌ Problema: "Failed to fetch" ou CORS error no Swagger

**Erro completo:**
```
Failed to fetch. 
Possible Reasons:
- CORS
- Network Failure  
- URL scheme must be "http" or "https" for CORS request.
```

**Causa:** CORS não configurado

**✅ Solução (JÁ APLICADA):**

CORS já foi configurado! Se ainda tiver erro:

1. **Verificar se API está rodando:**
   ```bash
   curl http://localhost:5076/health
   ```

2. **Reiniciar a API:**
   ```bash
   # Parar (Ctrl+C)
   # Rodar novamente
   cd src/ExamAI.Api
   dotnet run
   ```

3. **Limpar cache do navegador:**
   - Ctrl+Shift+Delete
   - Limpar cache
   - Recarregar Swagger (Ctrl+F5)

4. **Testar em navegador anônimo/privado**

**Configuração CORS (já aplicada em Program.cs):**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// E no pipeline:
app.UseCors();  // Antes de UseHttpsRedirection
```

---

### ❌ Problema: API não inicia (porta ocupada)

**Verificar porta 5076:**
```bash
netstat -ano | findstr :5076
```

**Alterar porta:**
```json
// appsettings.json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5080"  // Porta diferente
      }
    }
  }
}
```

---

### ❌ Problema: CORS errors

**Se acessar de frontend externo:**

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Depois de app.UseHttpsRedirection()
app.UseCors();
```

---

## 🔐 pgAdmin

### ❌ Problema: Não conecta ao PostgreSQL

**Dentro do Docker (hostname `postgres`):**
- Host: `postgres`
- Port: `5432`

**Fora do Docker (hostname `localhost`):**
- Host: `localhost`  
- Port: `5432`

**Verificar rede:**
```bash
docker network ls
docker network inspect examai_examai-network
```

---

### ❌ Problema: Esqueci a senha do pgAdmin

**Resetar:**

1. Parar containers:
   ```bash
   docker-compose down
   ```

2. Remover volume do pgAdmin:
   ```bash
   docker volume rm examai_pgadmin_data
   ```

3. Reiniciar:
   ```bash
   docker-compose up -d
   ```

4. Nova senha: `admin123` (padrão)

---

## 📊 Geral

### ❌ Problema: Tudo quebrou, quero recomeçar

**Reset COMPLETO (⚠️ APAGA TUDO):**

```bash
# 1. Parar tudo
docker-compose down -v

# 2. Limpar builds
dotnet clean
rm -rf src/*/bin src/*/obj

# 3. Remover migrations
rm -rf src/ExamAI.Infrastructure/Migrations

# 4. Reiniciar do zero
docker-compose up -d

# 5. Aguardar PostgreSQL (~30s)
docker-compose logs -f postgres

# 6. Criar migration
cd src/ExamAI.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../ExamAI.Api

# 7. Aplicar migration
cd ../ExamAI.Api
dotnet ef database update

# 8. Rodar
dotnet run
```

---

## 🆘 Comandos de Diagnóstico

### Verificar tudo

```bash
# Docker
docker --version
docker info
docker-compose --version

# .NET
dotnet --version
dotnet --list-sdks

# Ollama
curl http://localhost:11434/api/tags

# PostgreSQL
docker exec -it examai-postgres pg_isready -U postgres

# API
curl http://localhost:5076/health
```

---

### Logs úteis

```bash
# Docker Compose
docker-compose logs -f

# PostgreSQL
docker-compose logs -f postgres

# pgAdmin
docker-compose logs -f pgadmin

# API
cd src/ExamAI.Api
dotnet run --verbosity detailed
```

---

## 📞 Precisa de Mais Ajuda?

1. ✅ Ver [README.md](README.md) - Documentação principal
2. ✅ Ver [QUICK-START.md](QUICK-START.md) - Guia rápido
3. ✅ Ver [docker/README.md](docker/README.md) - Docker completo
4. ✅ Abrir issue no GitHub
5. ✅ Verificar logs: `docker-compose logs -f`

---

**Desenvolvido por:** Adjair Farias + Clawdex 🔍  
**Versão:** 1.2.0  
**Última atualização:** 04/02/2026
