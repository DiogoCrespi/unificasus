# Guia de Início Rápido - UnificaSUS

## 🚀 Início Rápido

### 1. Pré-requisitos

Certifique-se de ter instalado:
- ✅ .NET 8 SDK ([Download](https://dotnet.microsoft.com/download))
- ✅ Firebird 5.0 ([Download](https://firebirdsql.org/en/downloads/))
- ✅ Visual Studio 2022 ou VS Code (opcional)

### 2. Configurar Banco de Dados

#### Verificar arquivo de configuração

O arquivo `unificasus.ini` deve estar em:
```
C:\Program Files\claupers\unificasus\unificasus.ini
```

#### Formato do arquivo

```ini
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

**Nota**: Se estiver usando banco remoto, use o formato:
```ini
[DB]
local=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB
```

### 3. Compilar o Projeto

Abra o terminal na pasta do projeto:
```powershell
cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
```

Restaurar pacotes NuGet:
```powershell
dotnet restore
```

Compilar:
```powershell
dotnet build
```

### 4. Executar a Aplicação

#### Opção 1: Via linha de comando
```powershell
dotnet run --project src\UnificaSUS.WPF\UnificaSUS.WPF.csproj
```

#### Opção 2: Via Visual Studio
1. Abra o arquivo `UnificaSUS.sln` no Visual Studio
2. Pressione `F5` ou clique em "Iniciar"

#### Opção 3: Executar executável (após build)
```powershell
dotnet build -c Release
cd src\UnificaSUS.WPF\bin\Release\net8.0-windows
.\UnificaSUS.WPF.exe
```

## 🔧 Configuração Adicional

### Credenciais do Banco

Por padrão, a aplicação usa:
- **Usuário**: `SYSDBA`
- **Senha**: `masterkey`

Para alterar, modifique o arquivo `ConfigurationReader.cs`:
```csharp
private const string DefaultUser = "SEU_USUARIO";
private const string DefaultPassword = "SUA_SENHA";
```

**⚠️ Importante**: Em produção, considere usar credenciais seguras!

### Competência Padrão

Atualmente, a aplicação usa a competência `"202401"` como padrão.

Para alterar ou implementar leitura dinâmica:
1. Modifique `MainWindow.xaml.cs`
2. Ou implemente leitura da tabela `TB_COMPETENCIA_ATIVA`

## 📝 Estrutura do Projeto

```
Projeto/
├── src/
│   ├── UnificaSUS.Core/          # Entidades e interfaces
│   ├── UnificaSUS.Infrastructure/ # Acesso a dados
│   ├── UnificaSUS.Application/    # Serviços de aplicação
│   └── UnificaSUS.WPF/            # Interface do usuário
├── docs/                          # Documentação
└── README.md                      # Documentação principal
```

## 🐛 Troubleshooting

### Erro: Arquivo de configuração não encontrado

**Solução**: Verifique se o arquivo `unificasus.ini` existe no caminho:
```
C:\Program Files\claupers\unificasus\unificasus.ini
```

### Erro: Falha na conexão com o banco

**Possíveis causas**:
1. Firebird não está rodando (se usar servidor)
2. Caminho do banco incorreto
3. Credenciais incorretas
4. Banco está em uso por outra aplicação

**Soluções**:
- Verifique se o Firebird Server está rodando
- Verifique se o caminho do banco no `.ini` está correto
- Verifique as credenciais
- Feche outras aplicações que possam estar usando o banco

### Erro: .NET SDK não encontrado

**Solução**: Instale o .NET 8 SDK:
https://dotnet.microsoft.com/download

### Erro: Pacote NuGet não encontrado

**Solução**: Restaure os pacotes:
```powershell
dotnet restore
```

## 📚 Próximos Passos

1. ✅ Projeto compilado e funcionando
2. ⏭️ Implementar seleção de competência
3. ⏭️ Adicionar filtros avançados
4. ⏭️ Implementar navegação hierárquica (TreeView)
5. ⏭️ Adicionar CRUD completo

## 📖 Documentação

Consulte:
- `README.md` - Visão geral
- `docs/ARQUITETURA.md` - Arquitetura detalhada
- `docs/BANCO_DADOS.md` - Estrutura do banco
- `docs/CONFIGURACAO.md` - Configuração detalhada
- `RESUMO_PROJETO.md` - Resumo completo

---

**Boa sorte no desenvolvimento! 🚀**

