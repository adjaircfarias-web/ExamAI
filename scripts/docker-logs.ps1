# Script para ver logs do Docker do ExamAI
# Uso: .\scripts\docker-logs.ps1 [-Service postgres|pgadmin] [-Follow]

param(
    [string]$Service = "",
    [switch]$Follow = $false
)

Write-Host "📋 Logs do ExamAI Docker" -ForegroundColor Cyan
Write-Host ""

# Verificar se Docker Compose v2
$useComposeV2 = $false
try {
    docker compose version | Out-Null
    $useComposeV2 = $true
} catch {
    # Usa docker-compose
}

# Montar comando
$cmd = if ($useComposeV2) { "docker compose logs" } else { "docker-compose logs" }

if ($Follow) {
    $cmd += " -f"
}

if ($Service) {
    $cmd += " $Service"
    Write-Host "👀 Exibindo logs de: $Service" -ForegroundColor Yellow
} else {
    Write-Host "👀 Exibindo logs de todos os serviços" -ForegroundColor Yellow
}

if ($Follow) {
    Write-Host "🔄 Modo follow ativado (Ctrl+C para sair)" -ForegroundColor Green
}

Write-Host ""
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

# Executar
Invoke-Expression $cmd
