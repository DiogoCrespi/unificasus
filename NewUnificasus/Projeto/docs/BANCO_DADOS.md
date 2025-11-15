# Banco de Dados - UnificaSUS

## 📊 Visão Geral

Banco de dados Firebird 5.0 contendo tabelas unificadas de procedimentos do SUS (DATASUS).

**Localização**: `C:\Program Files\claupers\unificasus\UNIFICASUS.GDB`

**ODS**: 13.1 (Firebird 5.0)

**Charset**: WIN1252

## 🗂️ Estrutura de Tabelas

### Tabelas Principais (TB_*)

#### TB_PROCEDIMENTO
Tabela principal de procedimentos do SUS.

**Campos principais**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK) - Código do procedimento
- `NO_PROCEDIMENTO` (VARCHAR(250)) - Nome do procedimento
- `VL_SA` (NUMERIC(10,2)) - Valor Serviço Ambulatorial
- `VL_SH` (NUMERIC(10,2)) - Valor Serviço Hospitalar
- `VL_SP` (NUMERIC(10,2)) - Valor Serviço Profissional
- `DT_COMPETENCIA` (VARCHAR(6)) - Data de competência (AAAAMM)
- `CO_FINANCIAMENTO` (VARCHAR(2)) - FK para TB_FINANCIAMENTO
- `CO_RUBRICA` (VARCHAR(6)) - FK para TB_RUBRICA

#### TB_CID
Classificação Internacional de Doenças.

**Campos principais**:
- `CO_CID` (VARCHAR(4), PK) - Código CID
- `NO_CID` (VARCHAR(100)) - Nome/Descrição do CID
- `TP_AGRAVO` (CHAR(1))
- `TP_SEXO` (CHAR(1))
- `TP_ESTADIO` (CHAR(1))

#### TB_FINANCIAMENTO
Tipos de financiamento.

**Campos principais**:
- `CO_FINANCIAMENTO` (VARCHAR(2), PK)
- `NO_FINANCIAMENTO` (VARCHAR(100))
- `DT_COMPETENCIA` (VARCHAR(6))

#### TB_RUBRICA
Rubricas.

**Campos principais**:
- `CO_RUBRICA` (VARCHAR(6), PK)
- `NO_RUBRICA` (VARCHAR(100))
- `DT_COMPETENCIA` (VARCHAR(6))

#### TB_SERVICO
Serviços.

**Campos principais**:
- `CO_SERVICO` (VARCHAR(3), PK)
- `NO_SERVICO` (VARCHAR(120))
- `DT_COMPETENCIA` (VARCHAR(6))

#### TB_MODALIDADE
Modalidades de atendimento.

**Campos principais**:
- `CO_MODALIDADE` (VARCHAR(2), PK)
- `NO_MODALIDADE` (VARCHAR(100))
- `DT_COMPETENCIA` (VARCHAR(6))

#### TB_COMPETENCIA_ATIVA
Controla qual competência está ativa no sistema.

**Campos principais**:
- `DT_COMPETENCIA` (VARCHAR(6), PK)
- `ST_ATIVA` (CHAR(1)) - 'S' ou 'N'

### Tabelas Relacionais (RL_*)

#### RL_PROCEDIMENTO_CID
Relaciona procedimentos com CID.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_CID` (VARCHAR(4), PK, FK)
- `ST_PRINCIPAL` (CHAR(1))
- `DT_COMPETENCIA` (VARCHAR(6))

#### RL_PROCEDIMENTO_SERVICO
Relaciona procedimentos com serviços.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_SERVICO` (VARCHAR(3), PK, FK)
- `CO_CLASSIFICACAO` (VARCHAR(3), PK, FK)
- `DT_COMPETENCIA` (VARCHAR(6))

#### RL_PROCEDIMENTO_MODALIDADE
Relaciona procedimentos com modalidades.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_MODALIDADE` (VARCHAR(2), PK, FK)
- `DT_COMPETENCIA` (VARCHAR(6))

#### RL_PROCEDIMENTO_DETALHE
Relaciona procedimentos com detalhes.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_DETALHE` (VARCHAR(3), PK, FK)
- `DT_COMPETENCIA` (VARCHAR(6))

#### RL_PROCEDIMENTO_REGISTRO
Relaciona procedimentos com registros.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_REGISTRO` (VARCHAR(2), PK, FK)
- `DT_COMPETENCIA` (VARCHAR(6))

#### RL_PROCEDIMENTO_OCUPACAO
Relaciona procedimentos com ocupações.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_OCUPACAO` (VARCHAR(6), PK, FK)
- `DT_COMPETENCIA` (VARCHAR(6))

#### RL_PROCEDIMENTO_HABILITACAO
Relaciona procedimentos com habilitações.

**Campos**:
- `CO_PROCEDIMENTO` (VARCHAR(10), PK, FK)
- `CO_HABILITACAO` (VARCHAR(4), PK, FK)
- `NU_GRUPO_HABILITACAO` (VARCHAR(4))
- `DT_COMPETENCIA` (VARCHAR(6))

### Outras Tabelas Importantes

- `TB_GRUPO` - Grupos de procedimentos
- `TB_SUB_GRUPO` - Sub-grupos
- `TB_FORMA_ORGANIZACAO` - Formas de organização
- `TB_DESCRICAO` - Descrições detalhadas de procedimentos
- `TB_DESCRICAO_DETALHE` - Descrições de detalhes
- `TB_TUSS` - Tabela TUSS
- `TB_RENASES` - Tabela RENASES
- `RL_PROCEDIMENTO_COMPATIVEL` - Procedimentos compatíveis
- `RL_PROCEDIMENTO_INCREMENTO` - Incrementos de valores
- `TB_COMPONENTE_REDE` - Componentes de rede de atenção
- `RL_PROCEDIMENTO_COMP_REDE` - Relação procedimento-componente rede

## 🔗 Relacionamentos Principais

```
TB_PROCEDIMENTO
  ├── 1:N → RL_PROCEDIMENTO_CID → TB_CID
  ├── 1:N → RL_PROCEDIMENTO_SERVICO → TB_SERVICO
  ├── 1:N → RL_PROCEDIMENTO_MODALIDADE → TB_MODALIDADE
  ├── N:1 → TB_FINANCIAMENTO
  ├── N:1 → TB_RUBRICA
  └── 1:1 → TB_DESCRICAO
```

## 📝 Queries Principais

### Buscar Procedimentos por Competência

```sql
SELECT pr.CO_PROCEDIMENTO,
       pr.NO_PROCEDIMENTO,
       pr.VL_SA,
       pr.VL_SH,
       pr.VL_SP,
       pr.DT_COMPETENCIA
FROM TB_PROCEDIMENTO pr
WHERE pr.DT_COMPETENCIA = :competencia
ORDER BY pr.CO_PROCEDIMENTO
```

### Buscar Procedimentos por CID

```sql
SELECT pr.CO_PROCEDIMENTO,
       pr.NO_PROCEDIMENTO,
       pc.ST_PRINCIPAL
FROM TB_PROCEDIMENTO pr
INNER JOIN RL_PROCEDIMENTO_CID pc ON pr.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
WHERE pc.DT_COMPETENCIA = :competencia
  AND pc.CO_CID = :cid
```

### Buscar Procedimentos por Serviço

```sql
SELECT pr.CO_PROCEDIMENTO,
       pr.NO_PROCEDIMENTO
FROM TB_PROCEDIMENTO pr
INNER JOIN RL_PROCEDIMENTO_SERVICO ps ON pr.CO_PROCEDIMENTO = ps.CO_PROCEDIMENTO
WHERE ps.DT_COMPETENCIA = :competencia
  AND ps.CO_SERVICO = :servico
```

## ⚙️ Configuração de Conexão

### String de Conexão Firebird

```
Database=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB;
User=SYSDBA;
Password=masterkey;
Charset=WIN1252;
Dialect=3;
```

**Nota**: O caminho do banco é lido do arquivo `unificasus.ini`.

## 🔍 Índices Importantes

Para melhor performance, considerar índices em:
- `TB_PROCEDIMENTO.DT_COMPETENCIA`
- `TB_PROCEDIMENTO.CO_PROCEDIMENTO`
- `RL_PROCEDIMENTO_CID.CO_CID`
- `RL_PROCEDIMENTO_CID.DT_COMPETENCIA`
- `RL_PROCEDIMENTO_SERVICO.CO_SERVICO`
- `RL_PROCEDIMENTO_SERVICO.DT_COMPETENCIA`

## 📋 Observações

1. O campo `DT_COMPETENCIA` está presente em quase todas as tabelas e é usado para versionamento dos dados
2. A tabela `TB_COMPETENCIA_ATIVA` controla qual competência está ativa no sistema
3. Muitos relacionamentos são N:N, usando tabelas intermediárias (RL_*)
4. Valores monetários usam `NUMERIC(10,2)`
5. Códigos geralmente são `VARCHAR` com tamanhos fixos

