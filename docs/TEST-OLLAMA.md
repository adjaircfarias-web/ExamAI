# ✅ Testes de Integração - Ollama

## 🎯 Objetivo

Verificar se a integração entre ExamAI e Ollama está funcionando corretamente.

---

## 📋 Pré-requisitos

Antes de testar, certifique-se:

- [ ] Ollama instalado
- [ ] Modelo `llama3.1:8b` baixado
- [ ] Ollama rodando em `http://localhost:11434`
- [ ] API ExamAI compilada sem erros

---

## 🧪 Teste 1: Verificar se Ollama está rodando

```bash
# Teste direto na API do Ollama
curl http://localhost:11434/api/tags
```

**Response esperada:**
```json
{
  "models": [
    {
      "name": "llama3.1:8b",
      "size": 4661224448,
      "digest": "...",
      "modified_at": "2026-02-03T01:00:00Z"
    }
  ]
}
```

**Se falhar:**
- Windows: Abra o aplicativo Ollama pelo menu Iniciar
- Linux/Mac: Execute `ollama serve`

---

## 🧪 Teste 2: Teste Interativo do Modelo

```bash
ollama run llama3.1:8b

>>> Olá, você fala português?
# Deve responder em português

>>> Liste 3 tipos de exames médicos
# Deve listar exames

>>> /bye
```

**✅ Passou:** Ollama está funcionando e responde em português

---

## 🧪 Teste 3: Health Check da API

### **Passo 1: Iniciar a API**

```bash
cd C:\dev\myprojects\ExamAI
dotnet run --project src/ExamAI.Api
```

**Output esperado:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### **Passo 2: Testar Health Check Geral**

```bash
# Em outro terminal
curl http://localhost:5000/health
```

**Response esperada:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-03T01:30:00Z",
  "version": "1.0.0"
}
```

### **Passo 3: Testar Health Check do Ollama**

```bash
curl http://localhost:5000/health/ollama
```

**Response esperada:**
```json
{
  "status": "healthy",
  "service": "Ollama",
  "url": "http://localhost:11434",
  "model": "llama3.1:8b",
  "timestamp": "2026-02-03T01:30:00Z"
}
```

**Se retornar status 503:**
```json
{
  "status": "unhealthy",
  "error": "Cannot connect to Ollama service. Is Ollama running?"
}
```

**Solução:** Inicie o Ollama e tente novamente

### **Passo 4: Testar Health Check do PostgreSQL**

```bash
curl http://localhost:5000/health/database
```

**Se PostgreSQL não estiver rodando:**
```json
{
  "status": "unhealthy",
  "error": "Cannot connect to database"
}
```

**Solução:** Suba o PostgreSQL (ver `docs/SETUP-POSTGRES.md`)

---

## 🧪 Teste 4: Teste via Swagger

### **Passo 1: Acessar Swagger**

1. Inicie a API: `dotnet run --project src/ExamAI.Api`
2. Abra o navegador: `http://localhost:5000/swagger`

### **Passo 2: Testar endpoints na UI**

1. Expanda `/health/ollama`
2. Clique em "Try it out"
3. Clique em "Execute"
4. Verifique a resposta

---

## 🧪 Teste 5: Teste de Inferência Básica (Manual)

Este teste será usado quando implementarmos os Agents.

**Exemplo de código (para referência futura):**

```csharp
// Isso será implementado no ExtractionAgent
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "Você é um assistente que extrai dados de exames médicos."),
    new(ChatRole.User, "Extraia os dados: Colesterol Total: 210 mg/dL")
};

var response = await chatClient.CompleteAsync(messages);
Console.WriteLine(response.Message.Text);
```

---

## 📊 Checklist de Validação

Marque conforme completa:

### **Setup**
- [ ] Ollama instalado e rodando
- [ ] Modelo llama3.1:8b baixado
- [ ] API ExamAI compila sem erros

### **Testes Ollama**
- [ ] `curl http://localhost:11434/api/tags` retorna 200
- [ ] `ollama run llama3.1:8b` funciona
- [ ] Modelo responde em português

### **Testes API**
- [ ] `GET /health` retorna 200
- [ ] `GET /health/ollama` retorna 200 (healthy)
- [ ] `GET /health/database` retorna 200 ou 503 (ok se PostgreSQL não estiver rodando ainda)

### **Logs**
- [ ] Logs mostram "Ollama client configured successfully"
- [ ] Sem erros de conexão nos logs

---

## 🐛 Troubleshooting

### Problema: "Cannot connect to Ollama service"

**Diagnóstico:**
```bash
# Verificar se Ollama está rodando
curl http://localhost:11434/api/tags
```

**Soluções:**
1. Iniciar Ollama: Abra o app Ollama ou execute `ollama serve`
2. Verificar porta: Certifique-se que 11434 não está bloqueada
3. Firewall: Permita conexões locais na porta 11434

### Problema: "Model not found"

**Diagnóstico:**
```bash
ollama list
```

**Solução:**
```bash
ollama pull llama3.1:8b
```

### Problema: API não inicia

**Diagnóstico:**
```bash
cd C:\dev\myprojects\ExamAI
dotnet build
```

**Soluções:**
1. Verificar erros de compilação
2. Limpar e rebuildar: `dotnet clean && dotnet build`
3. Verificar referências entre projetos

---

## ✅ Resultado Esperado

Após completar todos os testes:

```
✅ Ollama rodando
✅ Modelo llama3.1:8b disponível
✅ API compila sem erros
✅ Health checks retornam 200
✅ IChatClient configurado corretamente
```

---

## 🚀 Próximos Passos

Com Ollama validado, você pode:

1. ✅ Implementar parsers de documentos (US-005, US-006, US-007)
2. ✅ Implementar ExtractionAgent com Ollama (US-009)
3. ✅ Processar seu primeiro exame médico!

---

**Última atualização:** 03/02/2026  
**Status:** Pronto para testes
