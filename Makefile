# Makefile - ExamAI
# Comandos úteis para desenvolvimento
# Nota: Requer Make instalado (ou use scripts PowerShell)

.PHONY: help docker-up docker-down docker-logs migrate run test clean

help: ## Mostrar esta ajuda
	@echo "ExamAI - Comandos disponíveis:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'

docker-up: ## Iniciar Docker (PostgreSQL + pgAdmin)
	docker-compose up -d
	@echo "✅ Containers iniciados!"
	@echo "📊 PostgreSQL: localhost:5432"
	@echo "🎯 pgAdmin: http://localhost:5050"

docker-down: ## Parar Docker
	docker-compose down
	@echo "✅ Containers parados!"

docker-logs: ## Ver logs do Docker
	docker-compose logs -f

migrate: ## Aplicar migrations
	cd src/ExamAI.Api && dotnet ef database update
	@echo "✅ Migrations aplicadas!"

run: ## Rodar API
	cd src/ExamAI.Api && dotnet run

build: ## Build do projeto
	dotnet build ExamAI.slnx

test: ## Rodar testes (quando existirem)
	dotnet test

clean: ## Limpar build artifacts
	dotnet clean
	find . -name "bin" -type d -exec rm -rf {} +
	find . -name "obj" -type d -exec rm -rf {} +

reset: ## Reset completo (CUIDADO: apaga dados!)
	docker-compose down -v
	@echo "⚠️  Volumes removidos!"

setup: docker-up migrate ## Setup inicial completo
	@echo "✅ Setup completo!"
	@echo "🚀 Execute 'make run' para iniciar a API"

status: ## Status dos containers
	docker-compose ps
	@echo ""
	@echo "🔌 Ollama:"
	@curl -s http://localhost:11434/api/tags | head -1 || echo "❌ Ollama offline"
