# UnificaSUS - Nova Implementação

## 🎯 Objetivo

Refatorar e modernizar a aplicação UnificaSUS, mantendo compatibilidade com o banco de dados Firebird existente.

## 📋 Arquitetura

### Tecnologias Escolhidas

- **Linguagem**: C# (.NET 8)
- **Framework Desktop**: WPF (Windows Presentation Foundation)
- **Banco de Dados**: Firebird 5.0
- **ORM**: FirebirdClient (oficial)
- **Configuração**: Arquivo INI (`unificasus.ini`)

### Estrutura do Projeto

```
Projeto/
├── src/
│   ├── UnificaSUS.Core/          # Camada de domínio (entidades, interfaces)
│   ├── UnificaSUS.Infrastructure/ # Camada de dados (Firebird, repositórios)
│   ├── UnificaSUS.Application/    # Camada de aplicação (serviços, DTOs)
│   └── UnificaSUS.WPF/            # Camada de apresentação (UI)
├── tests/
│   ├── UnificaSUS.Core.Tests/
│   ├── UnificaSUS.Infrastructure.Tests/
│   └── UnificaSUS.Application.Tests/
└── docs/
    ├── ARQUITETURA.md
    ├── BANCO_DADOS.md
    └── CONFIGURACAO.md
```

## 🔧 Configuração

### Arquivo unificasus.ini

O arquivo `unificasus.ini` localizado em `C:\Program Files\claupers\unificasus\unificasus.ini` deve conter:

```ini
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

A aplicação lê este arquivo automaticamente para configurar a conexão com o banco de dados.

## 📊 Banco de Dados

### Estrutura

- **~40 tabelas principais** (TB_*)
- **~20 tabelas relacionais** (RL_*)
- **Banco**: Firebird 5.0
- **ODS**: 13.1

### Tabelas Principais

- `TB_PROCEDIMENTO` - Procedimentos do SUS
- `TB_CID` - Classificação Internacional de Doenças
- `TB_FINANCIAMENTO` - Tipos de financiamento
- `TB_RUBRICA` - Rubricas
- `TB_SERVICO` - Serviços
- `TB_MODALIDADE` - Modalidades
- `RL_PROCEDIMENTO_CID` - Relação Procedimento-CID
- `RL_PROCEDIMENTO_SERVICO` - Relação Procedimento-Serviço
- E muitas outras...

## 🚀 Funcionalidades Planejadas

### Fase 1 - MVP
- [x] Estrutura do projeto
- [ ] Leitura do arquivo de configuração (.ini)
- [ ] Conexão com banco Firebird
- [ ] Consulta básica de procedimentos
- [ ] Interface básica

### Fase 2 - Completo
- [ ] Importação de dados (TXT)
- [ ] Navegação hierárquica (TreeView)
- [ ] CRUD completo de procedimentos
- [ ] Busca e filtros avançados
- [ ] Verificações e validações

### Fase 3 - Refinamento
- [ ] Relatórios
- [ ] Exportação de dados
- [ ] Backup/restore
- [ ] Otimizações de performance

## 📝 Notas

- A aplicação trabalha com o banco localizado em `C:\Program Files\claupers\unificasus\UNIFICASUS.GDB`
- O arquivo de configuração permite fácil mudança de localização do banco
- Mantém compatibilidade total com a estrutura de dados existente

