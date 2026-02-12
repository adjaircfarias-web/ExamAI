# 🐳 Docker Setup - ExamAI

Docker configuration for the ExamAI project.

---

## 📦 What's Included

- **PostgreSQL 16 Alpine** - Optimized database
- **Persistent volumes** - Data is not lost when restarting
- **Health checks** - Monitors database health
- **Isolated network** - Secure communication between containers
- **ExamAI API** - Application container with all dependencies

---

## 🚀 Quick Start Compose (

### DockerRecommended)

```bash
# Start everything (PostgreSQL + API)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop everything
docker-compose down

# Stop and remove volumes (CAUTION: deletes data!)
docker-compose down -v
```

---

## 🔌 Connection

### Connection String

```
Host=localhost;Database=examai;Username=postgres;Password=postgres123;Port=5432
```

### Inside Docker Network

```
Host=postgres;Database=examai;Username=postgres;Password=postgres123;Port=5432
```

---

## 📊 Useful Commands

### Container Status
```bash
docker-compose ps
```

### Real-time Logs
```bash
# All containers
docker-compose logs -f

# Only API
docker-compose logs -f api

# Only PostgreSQL
docker-compose logs -f postgres
```

### Enter PostgreSQL Container
```bash
docker exec -it examai-postgres psql -U postgres -d examai
```

### Database Backup
```bash
docker exec -t examai-postgres pg_dump -U postgres examai > backup.sql
```

### Restore Backup
```bash
docker exec -i examai-postgres psql -U postgres examai < backup.sql
```

### Resource Usage
```bash
docker stats examai-postgres
docker stats examai-api
```

---

## 🔧 Customization

### Change PostgreSQL Port

```yaml
# docker-compose.yml
ports:
  - "15432:5432"  # Use port 15432 on host
```

### Change Credentials

```yaml
# docker-compose.yml
environment:
  POSTGRES_PASSWORD: your_secure_password
```

### Run Only PostgreSQL

```bash
# Start only PostgreSQL
docker-compose up -d postgres

# Access from local development
dotnet run
```

---

## 📁 File Structure

```
docker/
├── postgres/
│   ├── Dockerfile              # Custom PostgreSQL image
│   └── init/
│       └── 01-init.sql       # Initialization scripts
├── README.md                  # This documentation
docker-compose.yml            # Service orchestration
.env.example                  # Environment variables example
```

---

## 🔒 Security

### ⚠️ For Production:

1. **Change default passwords**
   ```bash
   POSTGRES_PASSWORD=strong_password_here
   ```

2. **Use Docker secrets**
   ```yaml
   secrets:
     postgres_password:
       file: ./secrets/postgres_password.txt
   ```

3. **Do not expose ports publicly**
   ```yaml
   # Only for internal network
   expose:
     - "5432"
   # No "ports:" section
   ```

4. **Use SSL certificates**
   ```yaml
   command: >
     postgres
     -c ssl=on
     -c ssl_cert_file=/etc/ssl/certs/server.crt
     -c ssl_key_file=/etc/ssl/private/server.key
   ```

---

## 🐛 Troubleshooting

### PostgreSQL Doesn't Start

```bash
# View detailed logs
docker-compose logs postgres

# Check health check status
docker inspect examai-postgres --format='{{.State.Health.Status}}'

# Remove volumes and recreate
docker-compose down -v
docker-compose up -d
```

### Port 5432 Already in Use

```bash
# Find what's using it
netstat -ano | findstr :5432

# Or change port in docker-compose.yml
ports:
  - "15432:5432"
```

### Data Doesn't Persist

```bash
# Check volumes
docker volume ls
docker volume inspect examai_postgres_data

# Ensure volume is mounted
docker inspect examai-postgres | grep -A 10 Mounts
```

### API Can't Connect to PostgreSQL

```bash
# Test PostgreSQL availability
docker exec examai-postgres pg_isready -U postgres

# Check network
docker network inspect examai_examai-network
```

### Ollama Connection Issues

```bash
# Test Ollama availability
curl http://localhost:11434/api/tags

# Check if model is downloaded
ollama list

# Pull model if needed
ollama pull phi4:14b
```

---

## 📚 Additional Resources

- [PostgreSQL Docs](https://www.postgresql.org/docs/)
- [Docker Compose Docs](https://docs.docker.com/compose/)
- [Ollama Docs](https://ollama.com/)

---

## 🎯 Setup Checklist

- [ ] Docker and Docker Compose installed
- [ ] `.env` file created (copy from `.env.example`)
- [ ] `docker-compose up -d` executed
- [ ] PostgreSQL accessible on port 5432
- [ ] Ollama model downloaded (phi4:14b or llama3.1:8b)
- [ ] API accessible at http://localhost:5076

---

**Developed by:** Adjair Farias  
**Version:** 1.4.0
