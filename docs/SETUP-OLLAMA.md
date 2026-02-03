# 🤖 Setup Ollama para ExamAI

## O que é Ollama?

Ollama permite rodar **Large Language Models (LLMs) localmente** no seu computador, sem enviar dados para a nuvem. Ideal para privacidade e custo zero por inferência.

---

## ✅ Pré-requisitos

- Windows 10/11, Linux ou macOS
- ~8GB RAM disponível
- ~6GB espaço em disco (para o modelo llama3.1:8b)
- GPU NVIDIA (opcional, mas recomendado para melhor performance)

---

## 📦 Instalação

### **Windows**

```powershell
# Opção 1: Instalador oficial
# Baixar de: https://ollama.com/download/windows

# Opção 2: Winget
winget install Ollama.Ollama

# Verificar instalação
ollama --version
```

### **Linux**

```bash
curl -fsSL https://ollama.com/install.sh | sh

# Verificar
ollama --version
```

### **macOS**

```bash
# Baixar de: https://ollama.com/download/mac
# Ou via Homebrew
brew install ollama

# Verificar
ollama --version
```

---

## 🚀 Baixar o Modelo

```bash
# Baixar Llama 3.1 8B (modelo recomendado)
ollama pull llama3.1:8b

# Verificar modelos instalados
ollama list
```

**Output esperado:**
```
NAME                ID              SIZE    MODIFIED
llama3.1:8b         abc123def456    4.7 GB  2 minutes ago
```

---

## ✅ Testar o Ollama

### **1. Teste Interativo**

```bash
ollama run llama3.1:8b

>>> Olá, você entende português?
Sim, eu entendo português! Como posso ajudá-lo?

>>> /bye
```

### **2. Testar API REST**

```bash
# Verificar se a API está rodando
curl http://localhost:11434/api/tags

# Testar geração de texto
curl http://localhost:11434/api/generate -d '{
  "model": "llama3.1:8b",
  "prompt": "Por que o céu é azul?",
  "stream": false
}'
```

---

## 🔌 Integração com ExamAI

### **1. Configuração no appsettings.json**

Já está configurado! Veja em `src/ExamAI.Api/appsettings.json`:

```json
{
  "Ollama": {
    "Url": "http://localhost:11434",
    "Model": "llama3.1:8b",
    "Temperature": 0.1,
    "MaxTokens": 4096,
    "TimeoutSeconds": 60
  }
}
```

### **2. Testar Health Check**

```bash
# Certifique-se que a API está rodando
cd C:\dev\myprojects\ExamAI
dotnet run --project src/ExamAI.Api

# Em outro terminal, teste o health check
curl http://localhost:5000/health/ollama
```

**Response esperada:**
```json
{
  "status": "healthy",
  "model": "llama3.1:8b",
  "responseTime": "2026-02-03T01:30:00Z",
  "response": "pong"
}
```

---

## 🐛 Troubleshooting

### **Erro: "connection refused"**

**Causa:** Ollama não está rodando

**Solução:**
```bash
# Windows: Ollama inicia automaticamente, mas se não estiver:
# Abra o aplicativo Ollama pelo menu Iniciar

# Linux/Mac: Iniciar o serviço
ollama serve
```

### **Erro: "model not found"**

**Causa:** Modelo não foi baixado

**Solução:**
```bash
ollama pull llama3.1:8b
```

### **Erro: "Out of memory" ou muito lento**

**Causa:** RAM insuficiente ou usando CPU em vez de GPU

**Solução:**

**1. Usar modelo menor:**
```bash
ollama pull llama3.1:3b  # Modelo menor, mais rápido
```

**2. Verificar se GPU está sendo usada:**
```bash
# NVIDIA
nvidia-smi

# Durante inferência, deve mostrar uso da GPU
```

**3. Forçar CPU-only (se GPU não funcionar):**
```bash
# Windows
$env:OLLAMA_NUM_GPU=0
ollama serve

# Linux/Mac
OLLAMA_NUM_GPU=0 ollama serve
```

### **Erro: "failed to allocate memory"**

**Causa:** Modelo muito grande para GPU disponível

**Solução:**
```bash
# Usar modelo quantizado (menor)
ollama pull llama3.1:8b-q4_0  # Versão quantizada

# Ou modelo menor
ollama pull llama3.1:3b
```

---

## 📊 Comparativo de Modelos

| Modelo | Tamanho | VRAM | RAM | Velocidade | Qualidade |
|--------|---------|------|-----|------------|-----------|
| **llama3.1:3b** | 2 GB | ~3 GB | ~4 GB | ⚡⚡⚡⚡⚡ | ⭐⭐⭐ |
| **llama3.1:8b** | 4.7 GB | ~6 GB | ~8 GB | ⚡⚡⚡⚡ | ⭐⭐⭐⭐ |
| **llama3.1:70b** | 40 GB | ~42 GB | ~64 GB | ⚡⚡ | ⭐⭐⭐⭐⭐ |

**Recomendação para ExamAI:** `llama3.1:8b` (melhor equilíbrio)

---

## 🔧 Configurações Avançadas

### **Alterar Modelo no ExamAI**

Edite `appsettings.json`:

```json
{
  "Ollama": {
    "Model": "llama3.1:3b"  // Trocar para modelo menor
  }
}
```

### **Ajustar Temperature**

- **Temperature = 0.0:** Mais determinístico (sempre mesma resposta)
- **Temperature = 0.1:** Levemente variável (recomendado para extração)
- **Temperature = 1.0:** Mais criativo

```json
{
  "Ollama": {
    "Temperature": 0.1  // Recomendado para dados estruturados
  }
}
```

### **Ver Logs do Ollama**

```bash
# Windows
%USERPROFILE%\.ollama\logs\server.log

# Linux/Mac
~/.ollama/logs/server.log

# Ver logs em tempo real
tail -f ~/.ollama/logs/server.log
```

---

## 🔐 Segurança

### **Ollama é Seguro?**

✅ **Sim!** Dados processados 100% localmente
- Nenhum dado enviado para a nuvem
- Sem telemetria
- Código open-source

### **LGPD Compliance**

✅ **Totalmente compatível:**
- Dados médicos nunca saem do servidor
- Sem transferência internacional de dados
- Sem processamento por terceiros

---

## 📚 Recursos Úteis

- **Documentação Oficial:** https://ollama.com/docs
- **Modelos Disponíveis:** https://ollama.com/library
- **GitHub:** https://github.com/ollama/ollama
- **Discord:** https://discord.gg/ollama

---

## ✅ Checklist de Verificação

Antes de continuar, verifique:

- [ ] Ollama instalado e rodando
- [ ] Modelo `llama3.1:8b` baixado
- [ ] API REST respondendo em `http://localhost:11434`
- [ ] Health check `/health/ollama` retorna 200 OK
- [ ] Teste interativo funciona (`ollama run llama3.1:8b`)

---

## 🚀 Próximos Passos

Com Ollama configurado, você pode:

1. ✅ Testar extração de texto de documentos
2. ✅ Implementar os Agents (ExtractionAgent, ValidationAgent, etc.)
3. ✅ Processar seus primeiros exames médicos

---

**Última atualização:** 03/02/2026  
**Versão Ollama recomendada:** 0.x.x  
**Modelo recomendado:** llama3.1:8b
