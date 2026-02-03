# 🔬 ExamAI

**API para extração automática de dados de exames médicos usando IA**

ExamAI é uma solução que utiliza Large Language Models (LLM) locais via Ollama para extrair informações estruturadas de documentos médicos em diversos formatos (PDF, Word, Excel), armazenando os dados de forma normalizada em PostgreSQL.

---

## 🎯 Características

- ✅ **Extração automática** de dados de exames clínicos
- ✅ **Múltiplos formatos** suportados (PDF, DOCX, XLSX)
- ✅ **IA local** usando Ollama (Llama 3.1) - 100% privado
- ✅ **Armazenamento estruturado** em PostgreSQL
- ✅ **API REST** para upload e consulta
- ✅ **Zero custo** por documento processado

---

## 📋 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 16+](https://www.postgresql.org/download/) ou Docker
- [Ollama](https://ollama.com) com modelo `llama3.1:8b` instalado

---

## 🚀 Quick Start

### 1. Clonar o repositório

```bash
git clone https://github.com/seu-usuario/ExamAI.git
cd ExamAI
```

### 2. Configurar PostgreSQL

**Via Docker:**
```bash
docker run --name examai-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=examai \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### 3. Verificar Ollama

```bash
# Verificar se Ollama está rodando
curl http://localhost:11434/api/tags

# Verificar se modelo está instalado
ollama list | grep llama3.1
```

### 4. Configurar appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=examai;Username=postgres;Password=postgres123"
  },
  "Ollama": {
    "Url": "http://localhost:11434",
    "Model": "llama3.1:8b"
  }
}
```

### 5. Executar migrations

```bash
dotnet ef database update --project src/ExamAI.Infrastructure --startup-project src/ExamAI.Api
```

### 6. Rodar a API

```bash
dotnet run --project src/ExamAI.Api
```

API disponível em: `http://localhost:5000`  
Swagger: `http://localhost:5000/swagger`

---

## 📁 Estrutura do Projeto

```
ExamAI/
├── src/
│   ├── ExamAI.Api/              # API REST (Controllers, Program.cs)
│   ├── ExamAI.Application/      # Lógica de negócio (Agents, Services)
│   ├── ExamAI.Domain/           # Entidades e interfaces
│   └── ExamAI.Infrastructure/   # Acesso a dados (EF Core, Parsers)
├── docs/                        # Documentação adicional
├── .gitignore
├── ExamAI.sln
└── README.md
```

---

## 🔌 Endpoints Principais

### Upload de Documento
```http
POST /api/exams/upload
Content-Type: multipart/form-data

file: [arquivo.pdf]
cpf: 12345678900
nomePaciente: João Silva
```

### Consultar Status
```http
GET /api/exams/status/{documentoId}
```

### Buscar Exames por Paciente
```http
GET /api/exams/paciente/{cpf}
```

### Health Check
```http
GET /health
GET /health/ollama
GET /health/database
```

---

## 🛠️ Tecnologias

- **.NET 10** - Framework principal
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **Ollama + Llama 3.1** - LLM local
- **iText7** - Parsing de PDF
- **DocumentFormat.OpenXml** - Parsing de Word
- **EPPlus** - Parsing de Excel

---

## 📊 Status do Projeto

🚧 **Em Desenvolvimento** - MVP em construção

### Implementado
- [x] Estrutura do projeto
- [ ] Modelo de dados
- [ ] Parsers de documentos
- [ ] Integração com Ollama
- [ ] Endpoints da API

### Próximos Passos
- [ ] Testes manuais com documentos reais
- [ ] Ajuste de prompts para melhor precisão
- [ ] Docker Compose para ambiente completo
- [ ] Documentação de API completa

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.

---

## 👤 Autor

**Adjair Farias**
- LinkedIn: [linkedin.com/in/farias-dev](https://linkedin.com/in/farias-dev)
- Email: adjaircfarias@gmail.com

---

## 🙏 Agradecimentos

- [Ollama](https://ollama.com) - Execução local de LLMs
- [Meta AI](https://ai.meta.com) - Llama 3.1
- Comunidade .NET

---

**Última atualização:** 02/02/2026
