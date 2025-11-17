# Processo de Identificação da Tabela TB_PROCOMUNS

## 📋 Objetivo

Identificar onde e como a funcionalidade "Proc. comuns" armazena seus dados, verificando se é no banco de dados ou em arquivos locais.

---

## 🔍 Passo 1: Análise do Código Existente

### 1.1 Verificação do Handler do Botão

**Arquivo**: `MainWindow.xaml.cs`

**Código encontrado**:
```csharp
private void ProcComuns_Click(object sender, RoutedEventArgs e)
{
    MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
}
```

**Conclusão**: A funcionalidade estava apenas com placeholder, não implementada.

---

## 🔍 Passo 2: Busca por Referências no Código

### 2.1 Busca por "comum", "Comum", "COMUM"
**Resultado**: Nenhuma referência encontrada no código.

### 2.2 Busca por "Observações", "Observacao"
**Resultado**: Nenhuma referência encontrada no código.

**Conclusão**: Não havia implementação prévia no código.

---

## 🔍 Passo 3: Verificação no Banco de Dados

### 3.1 Criação do Script SQL de Verificação

**Arquivo criado**: `verificar_tabelas_proc_comuns.sql`

**Objetivo**: Verificar se existem tabelas relacionadas a procedimentos comuns ou observações.

**Script criado**:
```sql
-- 1. Listar TODAS as tabelas do banco
SELECT RF.RDB$RELATION_NAME AS TABELA
FROM RDB$RELATIONS RF
WHERE RF.RDB$SYSTEM_FLAG = 0
  AND RF.RDB$RELATION_TYPE = 0
ORDER BY RF.RDB$RELATION_NAME;

-- 2. Procurar por tabelas relacionadas
-- Procurar por: COMUM, OBSERVACAO, FAVORITO, USUARIO, PERSONALIZADO, NOTA, ANOTACAO

-- 3. Verificar campos em TB_PROCEDIMENTO
-- Procurar por campos: OBSERVACAO, NOTA, COMUM, FAVORITO

-- 4. Verificar estrutura completa de TB_PROCEDIMENTO
```

### 3.2 Execução do Script

**Ferramenta usada**: `isql.exe` (Firebird Interactive SQL)

**Comando PowerShell**:
```powershell
$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$IsqlPath = Join-Path $FirebirdPath "isql.exe"
& $IsqlPath -user SYSDBA -password masterkey $DatabasePath -i verificar_tabelas_proc_comuns.sql
```

**Resultado**: Lista de todas as tabelas do banco, incluindo **`TB_PROCOMUNS`** ✅

---

## 🔍 Passo 4: Análise da Tabela TB_PROCOMUNS

### 4.1 Verificação da Estrutura

**Script criado**: `verificar_tb_procomuns.sql`

**Comando SQL**:
```sql
SELECT 
    RF.RDB$FIELD_NAME AS CAMPO,
    RF.RDB$FIELD_SOURCE AS TIPO_ORIGEM,
    CASE 
        WHEN F.RDB$FIELD_TYPE = 7 THEN 'SMALLINT'
        WHEN F.RDB$FIELD_TYPE = 8 THEN 'INTEGER'
        WHEN F.RDB$FIELD_TYPE = 37 THEN 'VARCHAR'
        -- ... outros tipos
    END AS TIPO,
    F.RDB$FIELD_LENGTH AS TAMANHO
FROM RDB$RELATION_FIELDS RF
LEFT JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
WHERE RF.RDB$RELATION_NAME = 'TB_PROCOMUNS'
ORDER BY RF.RDB$FIELD_POSITION;
```

**Resultado**:
| Campo | Tipo | Tamanho | Descrição |
|-------|------|---------|-----------|
| `PRC_COD` | INTEGER | 4 | Chave primária (NOT NULL) |
| `PRC_CODPROC` | VARCHAR | 10 | Código do procedimento |
| `PRC_NO_PROCEDIMENTO` | VARCHAR | 250 | Nome do procedimento |
| `PRC_OBSERVACOES` | VARCHAR | 255 | **Campo de observações** ✅ |

### 4.2 Verificação de Índices e Constraints

**Resultado**:
- **Chave Primária**: `PRC_COD`
- **Índice**: `RDB$PRIMARY42` (único)

### 4.3 Verificação de Dados Existentes

**Script**: `ver_dados_tb_procomuns.sql`

**Resultado**:
- **Total de registros**: 4 procedimentos comuns
- **Exemplo de dados**:
  - `PRC_COD`: 1
  - `PRC_CODPROC`: "0101020040"
  - `PRC_NO_PROCEDIMENTO`: "AÇÃO COLETIVA DE EXAME BUCAL..."
  - `PRC_OBSERVACOES`: "Teste"

---

## 📊 Estrutura Final Identificada

### Tabela: `TB_PROCOMUNS`

```sql
CREATE TABLE TB_PROCOMUNS (
    PRC_COD INTEGER NOT NULL PRIMARY KEY,           -- Código único do registro
    PRC_CODPROC VARCHAR(10),                        -- Código do procedimento (FK para TB_PROCEDIMENTO.CO_PROCEDIMENTO)
    PRC_NO_PROCEDIMENTO VARCHAR(250),               -- Nome do procedimento
    PRC_OBSERVACOES VARCHAR(255)                    -- Observações do usuário
);
```

### Relacionamento

- `PRC_CODPROC` → `TB_PROCEDIMENTO.CO_PROCEDIMENTO` (relação lógica, não há FK física)

---

## ✅ Conclusões

1. **Armazenamento**: Os dados são salvos **no banco de dados**, não localmente
2. **Tabela**: `TB_PROCOMUNS` já existe e contém dados
3. **Campo de Observações**: Existe e está sendo usado (`PRC_OBSERVACOES`)
4. **Estrutura**: Tabela simples com 4 campos, chave primária numérica

---

## 🛠️ Ferramentas Utilizadas

1. **isql.exe** (Firebird Interactive SQL) - Linha de comando
2. **PowerShell** - Automação de execução
3. **Scripts SQL** - Consultas de metadados do Firebird

---

## 📝 Scripts Criados

1. `verificar_tabelas_proc_comuns.sql` - Lista todas as tabelas e busca por nomes relacionados
2. `verificar_tb_procomuns.sql` - Analisa estrutura completa da tabela
3. `ver_dados_tb_procomuns.sql` - Visualiza dados existentes
4. `executar_verificacao_proc_comuns.ps1` - Script PowerShell para automação

---

## 🎯 Próximos Passos (Implementação)

1. ✅ Criar entidade `ProcedimentoComum` no Core
2. ✅ Criar repositório `IProcedimentoComumRepository` e implementação
3. ✅ Criar serviço `ProcedimentoComumService`
4. ✅ Implementar interface no `MainWindow.xaml.cs`
5. ✅ Criar diálogo para adicionar/editar procedimentos comuns
6. ✅ Implementar CRUD completo (Create, Read, Update, Delete)

---

## 🔧 Teste Manual e Correção de Deadlock

### Problema Identificado

Durante o teste manual de inserção/atualização, foi encontrado o seguinte erro:

```
Erro ao atualizar procedimento comum:
deadlock
update conflicts with concurrent update
concurrent transaction number is 76010
```

### Causa do Problema

O erro de deadlock ocorreu porque as operações de escrita (INSERT, UPDATE, DELETE) não estavam usando transações explícitas. O Firebird pode ter problemas com múltiplas operações concorrentes na mesma conexão sem controle de transação adequado.

### Solução Aplicada

Foram adicionadas **transações explícitas** para todas as operações de escrita:

1. **AdicionarAsync**: Usa `BeginTransactionAsync` → `CommitAsync` / `RollbackAsync`
2. **AtualizarAsync**: Usa `BeginTransactionAsync` → `CommitAsync` / `RollbackAsync`
3. **RemoverAsync**: Usa `BeginTransactionAsync` → `CommitAsync` / `RollbackAsync`

**Padrão aplicado** (mesmo usado em `CompetenciaRepository`):
```csharp
using var transaction = await _context.BeginTransactionAsync(cancellationToken);

try
{
    using var command = new FbCommand(sql, _context.Connection, transaction);
    // ... configuração de parâmetros ...
    await command.ExecuteNonQueryAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch (Exception ex)
{
    await transaction.RollbackAsync(cancellationToken);
    _logger.LogError(ex, "Erro ao executar operação");
    throw;
}
```

### Verificação de Permissões

Para verificar se há permissões de INSERT/UPDATE/DELETE na tabela `TB_PROCOMUNS`:

```sql
-- Verificar permissões do usuário SYSDBA na tabela
SELECT 
    RF.RDB$RELATION_NAME AS TABELA,
    RF.RDB$USER AS USUARIO,
    RF.RDB$PRIVILEGE AS PRIVILEGIO
FROM RDB$USER_PRIVILEGES RF
WHERE RF.RDB$RELATION_NAME = 'TB_PROCOMUNS'
  AND RF.RDB$USER = 'SYSDBA';
```

**Nota**: O usuário `SYSDBA` normalmente tem todas as permissões por padrão no Firebird.

### Teste Manual Recomendado

1. **Teste de Inserção**:
   - Selecionar um procedimento na lista principal
   - Clicar em "Proc. comuns" → "Adicionar"
   - Adicionar observações e salvar
   - Verificar se foi inserido corretamente

2. **Teste de Atualização**:
   - Dar duplo clique na célula de "Observações"
   - Editar as observações
   - Salvar e verificar se foi atualizado

3. **Teste de Remoção**:
   - Selecionar um procedimento comum
   - Clicar em "Remover"
   - Confirmar e verificar se foi removido

### Status

✅ **Correção aplicada**: Transações explícitas implementadas em todas as operações de escrita

---

## 📚 Referências

- **Firebird System Tables**: `RDB$RELATIONS`, `RDB$RELATION_FIELDS`, `RDB$FIELDS`
- **Documentação Firebird**: https://firebirdsql.org/
- **isql Reference**: https://firebirdsql.org/file/documentation/html/en/refdocs/fblangref25/firebird-25-language-reference.html#fblangref25-appx05-isql

---

**Data da Identificação**: 2024
**Autor**: Processo automatizado de análise
**Versão do Banco**: Firebird 3.0

