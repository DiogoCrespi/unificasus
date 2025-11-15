# Arquitetura - UnificaSUS

## 🏗️ Visão Geral

Aplicação desktop Windows desenvolvida em C#/.NET 8 com arquitetura em camadas (Clean Architecture).

## 📦 Camadas

### 1. UnificaSUS.Core (Camada de Domínio)

**Responsabilidade**: Contém as entidades de negócio, interfaces e regras de domínio.

**Conteúdo**:
- Entidades (Procedimento, CID, Servico, etc.)
- Interfaces de repositórios
- Exceções de domínio
- Value Objects

**Dependências**: Nenhuma (camada mais interna)

### 2. UnificaSUS.Infrastructure (Camada de Infraestrutura)

**Responsabilidade**: Implementa acesso a dados e integrações externas.

**Conteúdo**:
- Implementação de repositórios
- Contexto de banco de dados (Firebird)
- Configurações de conexão
- Migrações (se necessário)

**Dependências**: UnificaSUS.Core

**Bibliotecas**:
- `FirebirdSql.Data.FirebirdClient` - Cliente oficial Firebird
- `System.Data` - ADO.NET

### 3. UnificaSUS.Application (Camada de Aplicação)

**Responsabilidade**: Lógica de aplicação, serviços e orquestração.

**Conteúdo**:
- Serviços de aplicação
- DTOs (Data Transfer Objects)
- Mappers
- Casos de uso

**Dependências**: UnificaSUS.Core

### 4. UnificaSUS.WPF (Camada de Apresentação)

**Responsabilidade**: Interface do usuário e interação.

**Conteúdo**:
- Views (XAML)
- ViewModels (MVVM)
- Converters
- Comandos

**Dependências**: 
- UnificaSUS.Application
- UnificaSUS.Infrastructure (apenas para inicialização)
- UnificaSUS.Core

**Bibliotecas**:
- `Microsoft.Extensions.DependencyInjection` - DI Container
- `Microsoft.Extensions.Configuration` - Configurações
- `CommunityToolkit.Mvvm` - MVVM Toolkit

## 🔄 Fluxo de Dados

```
UI (WPF) 
  ↓
Application Services
  ↓
Repository Interfaces (Core)
  ↓
Repository Implementations (Infrastructure)
  ↓
Firebird Database
```

## 📁 Estrutura de Diretórios

```
src/
├── UnificaSUS.Core/
│   ├── Entities/
│   │   ├── Procedimento.cs
│   │   ├── CID.cs
│   │   └── ...
│   ├── Interfaces/
│   │   ├── IProcedimentoRepository.cs
│   │   └── ...
│   └── Exceptions/
│       └── DomainException.cs
│
├── UnificaSUS.Infrastructure/
│   ├── Data/
│   │   ├── FirebirdContext.cs
│   │   └── ConfigurationReader.cs
│   ├── Repositories/
│   │   ├── ProcedimentoRepository.cs
│   │   └── ...
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
│
├── UnificaSUS.Application/
│   ├── Services/
│   │   ├── ProcedimentoService.cs
│   │   └── ...
│   ├── DTOs/
│   │   ├── ProcedimentoDTO.cs
│   │   └── ...
│   └── Mappings/
│       └── AutoMapperProfile.cs
│
└── UnificaSUS.WPF/
    ├── Views/
    │   ├── MainWindow.xaml
    │   └── ...
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   └── ...
    ├── Services/
    │   └── NavigationService.cs
    └── App.xaml.cs
```

## 🔌 Injeção de Dependências

```csharp
// App.xaml.cs
services.AddSingleton<IConfigurationReader, ConfigurationReader>();
services.AddScoped<IFirebirdContext, FirebirdContext>();
services.AddScoped<IProcedimentoRepository, ProcedimentoRepository>();
services.AddScoped<IProcedimentoService, ProcedimentoService>();
```

## 🔐 Configuração

O arquivo `unificasus.ini` é lido pela classe `ConfigurationReader`:

```ini
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

A string de conexão Firebird é construída automaticamente.

## 📊 Padrões Utilizados

1. **Repository Pattern** - Abstração de acesso a dados
2. **Unit of Work** - Gerenciamento de transações
3. **MVVM** - Padrão de apresentação (WPF)
4. **Dependency Injection** - Inversão de controle
5. **DTO Pattern** - Transferência de dados entre camadas

## 🧪 Testes

Estrutura de testes espelha a estrutura de código:

```
tests/
├── UnificaSUS.Core.Tests/
│   └── Entities/
├── UnificaSUS.Infrastructure.Tests/
│   └── Repositories/
└── UnificaSUS.Application.Tests/
    └── Services/
```

## 📚 Bibliotecas Principais

- **.NET 8** - Framework base
- **WPF** - Interface desktop
- **FirebirdSql.Data.FirebirdClient** - Cliente Firebird
- **Microsoft.Extensions.DependencyInjection** - DI Container
- **CommunityToolkit.Mvvm** - MVVM Toolkit
- **xUnit** - Framework de testes
- **Moq** - Mocking para testes

