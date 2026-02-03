# 🐘 Setup PostgreSQL para ExamAI

## Opção 1: Docker (Recomendado)

### Subir PostgreSQL via Docker

```bash
docker run --name examai-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=examai \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### Verificar se está rodando

```bash
docker ps | findstr examai-postgres
```

### Ver logs

```bash
docker logs examai-postgres
```

### Parar

```bash
docker stop examai-postgres
```

### Iniciar novamente

```bash
docker start examai-postgres
```

### Remover (CUIDADO: apaga dados)

```bash
docker rm -f examai-postgres
```

---

## Opção 2: PostgreSQL Local

### Download

Baixe o instalador em: https://www.postgresql.org/download/windows/

### Instalar

1. Execute o instalador
2. Configure a senha do usuário `postgres` como `postgres123`
3. Porta padrão: `5432`
4. Marque para instalar o **pgAdmin 4** (interface gráfica)

### Criar Database

Abra o pgAdmin 4 ou terminal:

```sql
CREATE DATABASE examai;
```

---

## Testando a Conexão

### Via Docker (psql)

```bash
docker exec -it examai-postgres psql -U postgres -d examai
```

### Via Terminal (Windows)

```powershell
# Adicionar ao PATH (se não instalou via Docker)
$env:PATH += ";C:\Program Files\PostgreSQL\16\bin"

# Conectar
psql -h localhost -U postgres -d examai
```

### Comandos úteis no psql

```sql
-- Listar databases
\l

-- Conectar ao database examai
\c examai

-- Listar tabelas
\dt

-- Sair
\q
```

---

## Connection String

A connection string configurada no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=examai;Username=postgres;Password=postgres123;Port=5432"
  }
}
```

**Componentes:**
- **Host:** localhost (ou IP do servidor)
- **Database:** examai (nome do banco)
- **Username:** postgres (usuário admin)
- **Password:** postgres123 (senha - **MUDE EM PRODUÇÃO!**)
- **Port:** 5432 (porta padrão PostgreSQL)

---

## Próximos Passos

Após o PostgreSQL estar rodando:

1. Execute as migrations para criar as tabelas:
   ```bash
   cd C:\dev\myprojects\ExamAI
   dotnet ef database update --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
   ```

2. Verifique se as tabelas foram criadas:
   ```sql
   \dt
   ```

---

## Troubleshooting

### Erro: "password authentication failed"
- Verifique se a senha está correta no `appsettings.json`
- No Docker, recrie o container com a senha correta

### Erro: "could not connect to server"
- Verifique se o PostgreSQL está rodando: `docker ps`
- Verifique se a porta 5432 não está bloqueada pelo firewall

### Erro: "database does not exist"
- Crie o database: `CREATE DATABASE examai;`

---

**Última atualização:** 02/02/2026
