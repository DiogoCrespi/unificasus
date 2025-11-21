# Scripts de Identificação de Entidades

Esta pasta contém scripts SQL e PowerShell para facilitar a identificação de entidades no banco de dados Firebird.

## 📋 Scripts SQL Disponíveis

### 1. `listar_todas_tabelas.sql`
Lista todas as tabelas do banco de dados.

**Uso direto**: Não requer parâmetros.

**Execução**:
```powershell
.\executar_verificacao.ps1 -ScriptNome "listar_todas_tabelas.sql"
```

---

### 2. `buscar_tabelas_por_termo.sql`
Busca tabelas que contenham um termo específico no nome.

**Uso**: Substituir `TERMO` no script ou usar o parâmetro `-Termo`.

**Exemplo**: Buscar tabelas relacionadas a CID
```powershell
.\executar_verificacao.ps1 -ScriptNome "buscar_tabelas_por_termo.sql" -Termo "CID"
```

---

### 3. `verificar_estrutura_tabela.sql`
Verifica a estrutura completa de uma tabela (campos, tipos, tamanhos).

**Uso**: Substituir `NOME_DA_TABELA` no script ou usar o parâmetro `-Tabela`.

**Exemplo**: Verificar estrutura de `TB_CID`
```powershell
.\executar_verificacao.ps1 -ScriptNome "verificar_estrutura_tabela.sql" -Tabela "TB_CID"
```

---

### 4. `verificar_indices_constraints.sql`
Verifica índices, chaves primárias e chaves estrangeiras de uma tabela.

**Uso**: Substituir `NOME_DA_TABELA` no script ou usar o parâmetro `-Tabela`.

**Exemplo**: Verificar índices de `TB_CID`
```powershell
.\executar_verificacao.ps1 -ScriptNome "verificar_indices_constraints.sql" -Tabela "TB_CID"
```

---

### 5. `ver_dados_tabela.sql`
Verifica dados existentes em uma tabela (contagem e amostra).

**Uso**: Substituir `NOME_DA_TABELA` no script ou usar o parâmetro `-Tabela`.

**Exemplo**: Ver dados de `TB_CID`
```powershell
.\executar_verificacao.ps1 -ScriptNome "ver_dados_tabela.sql" -Tabela "TB_CID"
```

---

### 6. `buscar_campos_por_termo.sql`
Busca campos em todas as tabelas que contenham um termo específico no nome.

**Uso**: Substituir `TERMO` no script ou usar o parâmetro `-Termo`.

**Exemplo**: Buscar campos relacionados a CID
```powershell
.\executar_verificacao.ps1 -ScriptNome "buscar_campos_por_termo.sql" -Termo "CID"
```

---

## 🔧 Script PowerShell: `executar_verificacao.ps1`

Script auxiliar para executar os scripts SQL com substituição automática de parâmetros.

### Parâmetros

- **`-ScriptNome`** (obrigatório): Nome do script SQL a executar
- **`-Tabela`** (opcional): Nome da tabela (substitui `NOME_DA_TABELA`)
- **`-Termo`** (opcional): Termo de busca (substitui `TERMO`)
- **`-FirebirdPath`** (opcional): Caminho do Firebird (padrão: `C:\Program Files\Firebird\Firebird_3_0`)
- **`-DatabasePath`** (opcional): Caminho do banco (padrão: `192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB`)
- **`-User`** (opcional): Usuário (padrão: `SYSDBA`)
- **`-Password`** (opcional): Senha (padrão: `masterkey`)

### Exemplos de Uso

#### 1. Listar todas as tabelas
```powershell
.\executar_verificacao.ps1 -ScriptNome "listar_todas_tabelas.sql"
```

#### 2. Buscar tabelas relacionadas a CBO
```powershell
.\executar_verificacao.ps1 -ScriptNome "buscar_tabelas_por_termo.sql" -Termo "CBO"
```

#### 3. Verificar estrutura de uma tabela
```powershell
.\executar_verificacao.ps1 -ScriptNome "verificar_estrutura_tabela.sql" -Tabela "TB_CBO"
```

#### 4. Verificar índices e constraints
```powershell
.\executar_verificacao.ps1 -ScriptNome "verificar_indices_constraints.sql" -Tabela "TB_CBO"
```

#### 5. Ver dados de uma tabela
```powershell
.\executar_verificacao.ps1 -ScriptNome "ver_dados_tabela.sql" -Tabela "TB_CBO"
```

#### 6. Buscar campos relacionados a um termo
```powershell
.\executar_verificacao.ps1 -ScriptNome "buscar_campos_por_termo.sql" -Termo "HABILITACAO"
```

---

## 📝 Processo Recomendado de Identificação

Para identificar uma nova entidade (ex: CID10), seguir esta sequência:

### Passo 1: Listar todas as tabelas
```powershell
.\executar_verificacao.ps1 -ScriptNome "listar_todas_tabelas.sql"
```
**Objetivo**: Ver todas as tabelas disponíveis no banco.

### Passo 2: Buscar tabelas relacionadas
```powershell
.\executar_verificacao.ps1 -ScriptNome "buscar_tabelas_por_termo.sql" -Termo "CID"
```
**Objetivo**: Encontrar tabelas que possam estar relacionadas ao termo.

### Passo 3: Buscar campos relacionados
```powershell
.\executar_verificacao.ps1 -ScriptNome "buscar_campos_por_termo.sql" -Termo "CID"
```
**Objetivo**: Encontrar campos em outras tabelas que possam estar relacionados.

### Passo 4: Verificar estrutura da tabela encontrada
```powershell
.\executar_verificacao.ps1 -ScriptNome "verificar_estrutura_tabela.sql" -Tabela "TB_CID"
```
**Objetivo**: Entender a estrutura completa da tabela.

### Passo 5: Verificar índices e constraints
```powershell
.\executar_verificacao.ps1 -ScriptNome "verificar_indices_constraints.sql" -Tabela "TB_CID"
```
**Objetivo**: Entender relacionamentos e chaves.

### Passo 6: Verificar dados existentes
```powershell
.\executar_verificacao.ps1 -ScriptNome "ver_dados_tabela.sql" -Tabela "TB_CID"
```
**Objetivo**: Ver exemplos de dados e quantidade de registros.

---

## 📊 Resultados

Todos os resultados são salvos automaticamente em arquivos de texto na mesma pasta, com o formato:
```
resultado_[nome_script]_[data_hora].txt
```

Exemplo: `resultado_verificar_estrutura_tabela_20241119_134500.txt`

---

## ⚠️ Observações Importantes

1. **Substituição de Parâmetros**: Os scripts SQL contêm placeholders (`NOME_DA_TABELA`, `TERMO`) que são substituídos automaticamente pelo script PowerShell.

2. **Encoding**: Os scripts SQL são salvos temporariamente em ASCII para compatibilidade com o Firebird, mas os resultados são salvos em UTF-8.

3. **Conexão Remota**: O script assume conexão remota ao banco. Ajuste `-DatabasePath` se necessário.

4. **Permissões**: Certifique-se de que o usuário tem permissões de leitura nas tabelas do sistema (`RDB$*`).

---

## 🔗 Referências

- **Firebird System Tables**: `RDB$RELATIONS`, `RDB$RELATION_FIELDS`, `RDB$FIELDS`, `RDB$INDICES`
- **Documentação Firebird**: https://firebirdsql.org/
- **isql Reference**: https://firebirdsql.org/file/documentation/html/en/refdocs/fblangref25/firebird-25-language-reference.html#fblangref25-appx05-isql

---

**Data de Criação**: 2024
**Versão do Banco**: Firebird 3.0

