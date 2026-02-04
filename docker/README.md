# 🐳 Docker Setup - ExamAI

Configuração Docker para o PostgreSQL do projeto ExamAI.

---

## 📦 O que está incluído

- **PostgreSQL 16 Alpine** - Banco de dados otimizado
- **pgAdmin 4** - Interface web para gerenciar o banco (opcional)
- **Volumes persistentes** - Dados não são perdidos ao reiniciar
- **Health checks** - Monitora saúde do banco
- **Rede isolada** - Comunicação segura entre containers

---

## 🚀 Quick Start

### Opção 1: Docker Compose (Recomendado)

```bash
# Subir tudo (PostgreSQL + pgAdmin)
docker-compose up -d

# Ver logs
docker-compose logs -f postgres

# Parar tudo
docker-compose down

# Parar e remover volumes (CUIDADO: apaga dados!)
docker-compose down -v
```

### Opção 2: Docker Manual

```bash
# Build da imagem
docker build -t examai-postgres ./docker/postgres

# Rodar container
docker run -d \
  --name examai-postgres \
  -e POSTGRES_DB=examai \
  -e POSTGRES_PASSWORD=postgres123 \
  -p 5432:5432 \
  -v examai_data:/var/lib/postgresql/data \
  examai-postgres

# Ver logs
docker logs -f examai-postgres
```

---

## 🔌 Conexão

### Connection String

```
Host=localhost;Database=examai;Username=postgres;Password=postgres123;Port=5432
```

### pgAdmin (se habilitado)

- **URL:** http://localhost:5050
- **Email:** admin@examai.com
- **Senha:** admin123

**Adicionar servidor no pgAdmin:**
1. Click com botão direito em "Servers" → "Register" → "Server"
2. **General** → Name: `ExamAI`
3. **Connection:**
   - Host: `postgres` (dentro do Docker) ou `localhost` (fora do Docker)
   - Port: `5432`
   - Database: `examai`
   - Username: `postgres`
   - Password: `postgres123`

---

## 📊 Comandos Úteis

### Status dos containers
```bash
docker-compose ps
```

### Logs em tempo real
```bash
docker-compose logs -f
```

### Entrar no container PostgreSQL
```bash
docker exec -it examai-postgres psql -U postgres -d examai
```

### Backup do banco
```bash
docker exec -t examai-postgres pg_dump -U postgres examai > backup.sql
```

### Restaurar backup
```bash
docker exec -i examai-postgres psql -U postgres examai < backup.sql
```

### Ver uso de recursos
```bash
docker stats examai-postgres
```

---

## 🔧 Customização

### Alterar porta do PostgreSQL

```yaml
# docker-compose.yml
ports:
  - "15432:5432"  # Usar porta 15432 no host
```

### Alterar credenciais

```yaml
# docker-compose.yml
environment:
  POSTGRES_PASSWORD: sua_senha_segura
```

**Ou usar arquivo .env:**

```bash
# .env
POSTGRES_PASSWORD=sua_senha_segura
```

### Desabilitar pgAdmin

```bash
# Subir apenas PostgreSQL
docker-compose up -d postgres
```

---

## 🗂️ Estrutura de Arquivos

```
docker/
├── postgres/
│   ├── Dockerfile              # Imagem customizada do PostgreSQL
│   └── init/
│       └── 01-init.sql         # Scripts de inicialização
├── README.md                   # Esta documentação
docker-compose.yml              # Orquestração dos serviços
.env.example                    # Exemplo de variáveis de ambiente
```

---

## 🔒 Segurança

### ⚠️ Para Produção:

1. **Alterar senhas padrão**
   ```bash
   POSTGRES_PASSWORD=senha_forte_aqui
   PGADMIN_PASSWORD=outra_senha_forte
   ```

2. **Usar secrets do Docker**
   ```yaml
   secrets:
     postgres_password:
       file: ./secrets/postgres_password.txt
   ```

3. **Não expor portas publicamente**
   ```yaml
   # Apenas para rede interna
   expose:
     - "5432"
   # Sem "ports:"
   ```

4. **Usar certificados SSL**
   ```yaml
   command: >
     postgres
     -c ssl=on
     -c ssl_cert_file=/etc/ssl/certs/server.crt
     -c ssl_key_file=/etc/ssl/private/server.key
   ```

---

## 🐛 Troubleshooting

### PostgreSQL não inicia

```bash
# Ver logs detalhados
docker-compose logs postgres

# Verificar health check
docker inspect examai-postgres --format='{{.State.Health.Status}}'

# Remover volumes e recriar
docker-compose down -v
docker-compose up -d
```

### Porta 5432 já está em uso

```bash
# Descobrir o que está usando
netstat -ano | findstr :5432

# Ou alterar porta no docker-compose.yml
ports:
  - "15432:5432"
```

### Dados não persistem

```bash
# Verificar volumes
docker volume ls
docker volume inspect examai_postgres_data

# Garantir que volume está montado
docker inspect examai-postgres | grep -A 10 Mounts
```

### pgAdmin não conecta ao PostgreSQL

**Dentro do Docker:** Use hostname `postgres`  
**Fora do Docker:** Use hostname `localhost`

```bash
# Testar conectividade
docker exec examai-postgres pg_isready -U postgres
```

---

## 📚 Recursos Adicionais

- [PostgreSQL Docs](https://www.postgresql.org/docs/)
- [Docker Compose Docs](https://docs.docker.com/compose/)
- [pgAdmin Docs](https://www.pgadmin.org/docs/)

---

## 🎯 Checklist de Setup

- [ ] Docker e Docker Compose instalados
- [ ] Arquivo `.env` criado (copiar de `.env.example`)
- [ ] `docker-compose up -d` executado
- [ ] PostgreSQL acessível na porta 5432
- [ ] Migrations aplicadas (`dotnet ef database update`)
- [ ] Conexão testada via pgAdmin ou `psql`

---

**Desenvolvido por:** Adjair Farias + Clawdex 🔍  
**Versão:** 1.1.0
