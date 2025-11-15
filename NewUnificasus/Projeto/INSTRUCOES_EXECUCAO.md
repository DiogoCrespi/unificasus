# Instruções de Execução - UnificaSUS

## ✅ Status Atual

### Validação Completa
- ✅ .NET 8.0 Runtime instalado (Microsoft.WindowsDesktop.App 8.0.22)
- ✅ .NET 9.0 SDK instalado (9.0.306) - Compatível com .NET 8.0
- ✅ Compilação bem-sucedida (0 erros, 0 avisos)
- ✅ Executável criado: `UnificaSUS.WPF.exe`
- ✅ Configuração atualizada: `unificasus.ini` apontando para banco local
- ✅ Banco de dados verificado: Existe no caminho especificado
- ✅ **Aplicação em execução** (processo detectado)

## 🚀 Como Executar a Aplicação

### Método 1: Script PowerShell (Recomendado)
```powershell
cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
.\EXECUTAR_APLICACAO.ps1
```

**Vantagens**:
- Validações automáticas
- Mensagens de erro claras
- Verificação de pré-requisitos

### Método 2: Script Batch
```cmd
cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
EXECUTAR_APLICACAO.bat
```

### Método 3: dotnet run
```powershell
cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
dotnet run --project src\UnificaSUS.WPF\UnificaSUS.WPF.csproj
```

### Método 4: Executar .exe diretamente
```powershell
cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\src\UnificaSUS.WPF\bin\Debug\net8.0-windows"
.\UnificaSUS.WPF.exe
```

## 🔧 Configuração

### Arquivo unificasus.ini

**Localização**: `C:\Program Files\claupers\unificasus\unificasus.ini`

**Configuração Atual**:
```ini
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

**Para mudar o banco**, edite o arquivo e altere o valor de `local`:
- Banco local: `localhost:C:\caminho\para\banco.GDB`
- Banco remoto: `192.168.0.3:E:\caminho\para\banco.GDB`

## 🔍 Verificações ao Iniciar

### 1. Verificar .NET
```powershell
dotnet --version
# Deve mostrar: 9.0.306 ou superior

dotnet --list-runtimes
# Deve incluir: Microsoft.WindowsDesktop.App 8.0.22 ou superior
```

### 2. Verificar Banco de Dados
```powershell
Test-Path "C:\Program Files\claupers\unificasus\UNIFICASUS.GDB"
# Deve retornar: True
```

### 3. Verificar Configuração
```powershell
Get-Content "C:\Program Files\claupers\unificasus\unificasus.ini"
# Deve mostrar a configuração do banco
```

### 4. Verificar Firebird
- Verificar se o Firebird Server está rodando (se usar servidor)
- Ou verificar se o Firebird Embedded está disponível (se usar embedded)

## ⚠️ Possíveis Problemas e Soluções

### Problema 1: Erro "Arquivo em uso"
**Causa**: Aplicação já está rodando
**Solução**: Feche a aplicação antes de recompilar

### Problema 2: Erro de conexão com banco
**Causa**: Firebird não está rodando ou banco inacessível
**Solução**: 
- Verificar se o Firebird Server está rodando
- Verificar se o caminho do banco está correto
- Verificar credenciais (SYSDBA/masterkey)

### Problema 3: Nenhuma competência encontrada
**Causa**: Não há competência ativa no banco
**Solução**: 
- Ativar uma competência usando o botão "ATIVAR COMPETÊNCIA"
- Ou inserir uma competência no banco:
  ```sql
  INSERT INTO TB_COMPETENCIA_ATIVA (DT_COMPETENCIA, ST_ATIVA, DT_ATIVACAO)
  VALUES ('202401', 'S', CURRENT_TIMESTAMP);
  ```

### Problema 4: TreeView vazio
**Causa**: Não há grupos cadastrados na competência ativa
**Solução**: 
- Verificar se há grupos no banco para a competência
- Consultar: `SELECT * FROM TB_GRUPO WHERE DT_COMPETENCIA = '{competencia}'`

### Problema 5: Grid de procedimentos vazio
**Causa**: Não há procedimentos na competência ativa
**Solução**: 
- Verificar se há procedimentos no banco
- Consultar: `SELECT COUNT(*) FROM TB_PROCEDIMENTO WHERE DT_COMPETENCIA = '{competencia}'`

## 📊 Estrutura de Execução

```
1. App.xaml.cs (OnStartup)
   ↓
2. Configurar DI Container
   ↓
3. Criar MainWindow
   ↓
4. MainWindow_Loaded
   ↓
5. CarregarCompetenciaAtivaAsync
   ↓
6. CarregarCompetenciasDisponiveisAsync
   ↓
7. CarregarGruposAsync (se competência ativa)
   ↓
8. CarregarProcedimentosSelecionadosAsync
```

## 🎯 Funcionalidades Disponíveis

### Ao Iniciar
- ✅ Carregamento automático de competência ativa
- ✅ Listagem de competências disponíveis
- ✅ Carregamento de grupos/categorias
- ✅ Carregamento de procedimentos

### Interface
- ✅ TreeView hierárquico (Grupos → Sub-grupos → Formas de Organização)
- ✅ Grid de procedimentos
- ✅ Campos de detalhes do procedimento
- ✅ ComboBox de competência
- ✅ Botões de navegação
- ✅ Busca de procedimentos

### Ações
- ✅ Ativar competência
- ✅ Buscar procedimentos
- ✅ Navegar entre procedimentos
- ✅ Visualizar detalhes do procedimento

## 📝 Logs e Debug

### Console Output
A aplicação usa `Microsoft.Extensions.Logging.Console`, então os logs aparecem no console se executar via `dotnet run`.

### Tratamento de Erros
- Erros de conexão: Mensagem clara ao usuário
- Erros de dados: Mensagem com detalhes
- Erros fatais: MessageBox com stack trace

## 🔄 Próximos Passos

1. **Testar Funcionalidades**:
   - Ativar competência
   - Navegar pelo TreeView
   - Buscar procedimentos
   - Visualizar detalhes

2. **Verificar Dados**:
   - Verificar se os dados são carregados corretamente
   - Verificar se as relações estão funcionando
   - Verificar se os filtros funcionam

3. **Melhorias Futuras**:
   - Implementar filtros avançados (CID, Serviços, etc.)
   - Implementar importação de dados
   - Implementar CRUD completo
   - Implementar relatórios

---

**Status**: ✅ **Aplicação pronta e em execução!**

**Data**: 14/11/2024

