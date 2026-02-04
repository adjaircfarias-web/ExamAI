# Script para iniciar o ambiente Docker do ExamAI
# Uso: .\scripts\docker-start.ps1

Write-Host "🐳 Iniciando ambiente Docker do ExamAI..." -ForegroundColor Cyan
Write-Host ""

# Verificar se Docker está instalado
try {
    $dockerVersion = docker --version
    Write-Host "✅ Docker detectado: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker não encontrado! Instale em: https://www.docker.com/get-started" -ForegroundColor Red
    exit 1
}

# Verificar se Docker está rodando
try {
    docker info | Out-Null
    Write-Host "✅ Docker Engine está rodando" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker Desktop NÃO está rodando!" -ForegroundColor Red
    Write-Host "" -ForegroundColor Yellow
    Write-Host "🔧 Solução:" -ForegroundColor Cyan
    Write-Host "  1. Abra o Docker Desktop" -ForegroundColor White
    Write-Host "  2. Aguarde até o ícone ficar verde" -ForegroundColor White
    Write-Host "  3. Execute este script novamente" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Dica: Procure 'Docker Desktop' no menu Iniciar" -ForegroundColor Yellow
    exit 1
}

# Verificar se Docker Compose está disponível
try {
    $composeVersion = docker-compose --version
    Write-Host "✅ Docker Compose detectado: $composeVersion" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Docker Compose não encontrado. Tentando 'docker compose'..." -ForegroundColor Yellow
    try {
        docker compose version
        Write-Host "✅ Docker Compose v2 detectado" -ForegroundColor Green
        $useComposeV2 = $true
    } catch {
        Write-Host "❌ Docker Compose não disponível!" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "📦 Subindo containers..." -ForegroundColor Cyan

# Subir containers
if ($useComposeV2) {
    docker compose up -d
} else {
    docker-compose up -d
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Containers iniciados com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Status dos containers:" -ForegroundColor Cyan
    
    if ($useComposeV2) {
        docker compose ps
    } else {
        docker-compose ps
    }
    
    Write-Host ""
    Write-Host "🎯 Acessos disponíveis:" -ForegroundColor Cyan
    Write-Host "  PostgreSQL: localhost:5432" -ForegroundColor White
    Write-Host "  pgAdmin:    http://localhost:5050" -ForegroundColor White
    Write-Host "    Email:    admin@examai.com" -ForegroundColor Gray
    Write-Host "    Senha:    admin123" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📖 Próximos passos:" -ForegroundColor Cyan
    Write-Host "  1. cd src\ExamAI.Api" -ForegroundColor White
    Write-Host "  2. dotnet ef database update" -ForegroundColor White
    Write-Host "  3. dotnet run" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Ver logs: docker-compose logs -f" -ForegroundColor Yellow
    Write-Host "🛑 Parar: docker-compose down" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ Erro ao iniciar containers!" -ForegroundColor Red
    Write-Host "💡 Dica: Execute 'docker-compose logs' para ver detalhes" -ForegroundColor Yellow
    exit 1
}
