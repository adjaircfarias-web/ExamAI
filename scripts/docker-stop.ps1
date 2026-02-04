# Script para parar o ambiente Docker do ExamAI
# Uso: .\scripts\docker-stop.ps1 [-RemoveVolumes]

param(
    [switch]$RemoveVolumes = $false
)

Write-Host "🛑 Parando ambiente Docker do ExamAI..." -ForegroundColor Cyan
Write-Host ""

# Verificar se Docker Compose v2
$useComposeV2 = $false
try {
    docker compose version | Out-Null
    $useComposeV2 = $true
} catch {
    # Usa docker-compose
}

# Parar containers
if ($RemoveVolumes) {
    Write-Host "⚠️  Removendo containers E volumes (dados serão apagados)..." -ForegroundColor Yellow
    if ($useComposeV2) {
        docker compose down -v
    } else {
        docker-compose down -v
    }
} else {
    Write-Host "📦 Parando containers (dados serão preservados)..." -ForegroundColor Cyan
    if ($useComposeV2) {
        docker compose down
    } else {
        docker-compose down
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Containers parados com sucesso!" -ForegroundColor Green
    
    if ($RemoveVolumes) {
        Write-Host "🗑️  Volumes removidos (dados apagados)" -ForegroundColor Yellow
    } else {
        Write-Host "💾 Volumes preservados (dados mantidos)" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "💡 Para iniciar novamente: .\scripts\docker-start.ps1" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "❌ Erro ao parar containers!" -ForegroundColor Red
    exit 1
}
