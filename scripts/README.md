# 🛠️ Scripts Utilitários - ExamAI

Scripts PowerShell para facilitar o gerenciamento do ambiente Docker.

---

## 📜 Scripts Disponíveis

### 1. `docker-start.ps1` - Iniciar Ambiente

Inicia todos os containers (PostgreSQL + pgAdmin).

```powershell
.\scripts\docker-start.ps1
```

**O que faz:**
- ✅ Verifica se Docker está instalado
- ✅ Sobe containers com `docker-compose up -d`
- ✅ Exibe status dos containers
- ✅ Mostra informações de acesso

---

### 2. `docker-stop.ps1` - Parar Ambiente

Para os containers (com ou sem remoção de volumes).

```powershell
# Parar containers (preservar dados)
.\scripts\docker-stop.ps1

# Parar containers E remover volumes (apagar dados)
.\scripts\docker-stop.ps1 -RemoveVolumes
```

**Parâmetros:**
- `-RemoveVolumes`: Remove os volumes (⚠️ **apaga dados permanentemente**)

---

### 3. `docker-logs.ps1` - Ver Logs

Exibe logs dos containers.

```powershell
# Ver logs de todos os serviços
.\scripts\docker-logs.ps1

# Ver logs de um serviço específico
.\scripts\docker-logs.ps1 -Service postgres
.\scripts\docker-logs.ps1 -Service pgadmin

# Seguir logs em tempo real (follow)
.\scripts\docker-logs.ps1 -Follow
.\scripts\docker-logs.ps1 -Service postgres -Follow
```

**Parâmetros:**
- `-Service <nome>`: Nome do serviço (postgres, pgadmin)
- `-Follow`: Modo follow (exibe logs em tempo real)

---

## 🎯 Exemplos de Uso

### Setup Inicial Completo

```powershell
# 1. Iniciar Docker
.\scripts\docker-start.ps1

# 2. Aplicar migrations
cd src\ExamAI.Api
dotnet ef database update

# 3. Ver logs do PostgreSQL
cd ..\..
.\scripts\docker-logs.ps1 -Service postgres

# 4. Rodar API
cd src\ExamAI.Api
dotnet run
```

---

### Desenvolvimento Diário

```powershell
# Manhã - iniciar ambiente
.\scripts\docker-start.ps1

# Durante o dia - ver logs se necessário
.\scripts\docker-logs.ps1 -Service postgres -Follow

# Fim do dia - parar containers
.\scripts\docker-stop.ps1
```

---

### Reset Completo (Limpar Dados)

```powershell
# ⚠️ CUIDADO: Remove TODOS os dados!
.\scripts\docker-stop.ps1 -RemoveVolumes

# Reiniciar do zero
.\scripts\docker-start.ps1
cd src\ExamAI.Api
dotnet ef database update
```

---

## 🔧 Troubleshooting

### "Execution Policy" Error

Se você receber erro sobre execution policy:

```powershell
# Temporariamente permitir scripts
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Executar script
.\scripts\docker-start.ps1
```

**Ou permitir permanentemente (admin):**
```powershell
Set-ExecutionPolicy RemoteSigned
```

---

### Porta 5432 em Uso

Se a porta 5432 já estiver ocupada:

1. Editar `docker-compose.yml`:
   ```yaml
   ports:
     - "15432:5432"  # Usar porta diferente
   ```

2. Atualizar `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=15432;..."
   }
   ```

---

### Containers Não Iniciam

```powershell
# Ver logs detalhados
.\scripts\docker-logs.ps1

# Reset completo
.\scripts\docker-stop.ps1 -RemoveVolumes
.\scripts\docker-start.ps1
```

---

## 📚 Comandos Docker Úteis

```powershell
# Status dos containers
docker-compose ps

# Entrar no container PostgreSQL
docker exec -it examai-postgres psql -U postgres -d examai

# Ver uso de recursos
docker stats

# Backup do banco
docker exec -t examai-postgres pg_dump -U postgres examai > backup.sql

# Restaurar backup
docker exec -i examai-postgres psql -U postgres examai < backup.sql
```

---

## 💡 Dicas

1. **Sempre preservar volumes** ao parar containers (padrão)
2. **Usar `-RemoveVolumes`** apenas quando quiser resetar tudo
3. **Logs em follow** são úteis para debug (`-Follow`)
4. **pgAdmin** é opcional, pode ser desabilitado no `docker-compose.yml`

---

**Desenvolvido por:** Adjair Farias + Clawdex 🔍  
**Versão:** 1.1.0
